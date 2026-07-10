using System.Collections;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Cosmos;

public class CosmosRoleRepository(CosmosClient cosmosClient, ILogger<CosmosRoleRepository> logger) : IRoleRepository
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown if required initialisation properties are <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Thrown if required parameters are <c>null</c>.</exception>
    public async Task<IResult<Role>> GetRolesAsync(GetRolesCriteria criteria, PagedCriteria? pagedCriteria = null)
    {
        using (logger.BeginScope("GetRolesAsync {criteria}, {pagedCriteria}", criteria, pagedCriteria?.ToString() ?? "null"))
        {
            // simple query for roles
            QueryDefinition queryDefinition = BuildQueryDefinitionForGetRoles(criteria);
            logger.LogDebug("QueryDefinition: {queryDefinition}", queryDefinition);
            
            CosmosReader<Role> cosmosReader;
            IResult<Role> result;
            var containerProperties = CreateRolesContainerProperties();
            if (pagedCriteria != null)
            {
                cosmosReader = new CosmosPagedReader<Role>(cosmosClient,
                    DatabaseNames.Core,
                    ContainerNames.Roles,
                    ContainerNames.RoleNamePartitionKey,
                    containerProperties);
                result = await ((CosmosPagedReader<Role>)cosmosReader).GetPagedItemsAsync(queryDefinition,
                    pagedCriteria.Offset,
                    pagedCriteria.ItemsPerPage);
            }
            else
            {
                cosmosReader = new CosmosReader<Role>(cosmosClient,
                    DatabaseNames.Core,
                    ContainerNames.Roles,
                    ContainerNames.RoleNamePartitionKey,
                    containerProperties);
                result = await cosmosReader.GetItemsAsync(queryDefinition);
            }

            logger.LogDebug("Result: {result}", result);
            
            result.IsRequiredToBeOrderedByClient = false;
            return result;
        }
    }
    
    /// <inheritdoc />
    public async Task<Role?> GetRoleByNameAsync(string roleName)
    {
        var roles = await GetRolesAsync(new GetRolesCriteria()
        {
            AnyOfRoleNames = [roleName]
        });
        if (roles.Items.Count() > 1)
        {
            logger.LogError("Expected zero or one, but more than one Role found with name {roleName}", roleName);
        }
        
        return roles.Items.SingleOrDefault();
    }

    /// <inheritdoc />
    /// <exception cref="ItemNotFoundException">Thrown if the Role does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown if the Role name is <c>null</c> or whitespace.</exception>
    /// <exception cref="ItemDeleteException">Thrown if the deletion failed.</exception>   
    public async Task DeleteRoleByNameAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException(nameof(roleName));

        var existingRole = await GetRoleByNameAsync(roleName);
        if (existingRole == null)
        {
            throw new ItemNotFoundException(OperationType.Update, typeof(Role), "Role does not exist");
        }
        
        // TODO: Cannot delete Role if it is required by an Application

        var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Roles);
        var response = await container.DeleteItemAsync<Role>(roleName, new PartitionKey(roleName));

        // TODO: Remove Role from all Users
        
        if (response.StatusCode != HttpStatusCode.NoContent)
        {
            throw new ItemDeleteException(typeof(Role), response.StatusCode);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ItemNotFoundException">Thrown if the Role does not exist.</exception>
    /// <exception cref="UpdateImmutablePropertyException">Thrown if the Role name is attempted to be changed.</exception>
    /// <exception cref="ItemUpdateException">Thrown if the update failed or was partial.</exception>   
    /// <exception cref="ArgumentException">Thrown if the Role name is <c>null</c> or whitespace.</exception>
    public async Task<Role> UpdateRoleAsync(string roleName, string? description, IEnumerable<string>? usersInRole)
    {
        if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException(nameof(roleName));

        var existingRole = await GetRoleByNameAsync(roleName);
        if (existingRole == null)
        {
            throw new ItemNotFoundException(OperationType.Update, typeof(Role), "Role does not exist");
        }

        // it is not possible to change the name of the role, because that is used as the partition key
        if (existingRole.RoleName != roleName)
        {
            throw new UpdateImmutablePropertyException(OperationType.Update, typeof(Role), "Attempt to change Role name prevented. Role Name is immutable.", "RoleName");           
        }
        
        existingRole.Description = description;
        
        var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Roles);
        var response = await container.ReplaceItemAsync(existingRole, existingRole.RoleName, new PartitionKey(roleName));

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new ItemUpdateException(typeof(Role), response.StatusCode);
        }
        
        if (usersInRole != null)
        {
            // get all users that have been assigned to the role and add to their record
            var usersAssignedToRoleQueryDefinition = BuildQueryDefinitionForUsersToAddToRole(roleName, usersInRole.ToArray());
            var usersAssignedToRole = await GetUsersAsync(usersAssignedToRoleQueryDefinition);
            var roleAssignmentFailures =
                new List<(string roleName, string userName, bool isAdd, bool isRemove, string message)>();

            foreach (var user in usersAssignedToRole)
            {
                try
                {
                    user.Roles = user.Roles.Union([roleName]);
                    await UpdateUserAsync(user);
                }
                catch (Exception e)
                {
                    roleAssignmentFailures.Add((roleName, user.UserName, true, false, e.Message));
                }
            }

            // get all users that have lost the role and remove from their record
            var usersLostRoleQueryDefinition = BuildQueryDefinitionForUsersToRemoveFromRole(roleName, usersInRole.ToArray());
            var usersLostRole = await GetUsersAsync(usersLostRoleQueryDefinition);
            foreach (var user in usersLostRole)
            {
                try
                {
                    user.Roles = user.Roles.Except([roleName]);
                    await UpdateUserAsync(user);
                }
                catch (Exception e)
                {
                    roleAssignmentFailures.Add((roleName, user.UserName, false, true, e.Message));
                }
            }

            if (roleAssignmentFailures.Any())
            {
                throw new ItemUpdateException(typeof(Role), "Failed to assign or remove role from users.");
            }
        }

        return existingRole;       
    }

    private async Task UpdateUserAsync(User user)
    {
        var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);
        var response = await container.ReplaceItemAsync(user, user.Id, new PartitionKey(user.UserName));

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new ItemUpdateException(typeof(Role), response.StatusCode);
        }
    }


    private async Task<List<User>> GetUsersAsync(QueryDefinition queryDefinition)
    {
        CosmosReader<User> cosmosReader;
        IResult<User> result;
        cosmosReader = new CosmosReader<User>(cosmosClient,
            DatabaseNames.Core,
            ContainerNames.Users,
            ContainerNames.UserNamePartitionKey);
        result = await cosmosReader.GetItemsAsync(queryDefinition);
        return result.Items.ToList();
    }


    private QueryDefinition BuildQueryDefinitionForUsersToAddToRole(string roleName, string[] usersNotInRoleAndWillGetIt)
    {
        var sb = new StringBuilder("SELECT c.id, c.userName, c.emailAddress, c.roles,c.type,c.versionNumber,c.firstName,c.lastName,c.profilePhotographSmall,c.profilePhotographOriginal,c.theme,c.createdAt,c.updatedAt,c.lastLoginAt,c.lastPasswordChangeAt FROM c WHERE 1=1");
        var parameters = new List<(string name, object value)>();
        
        // users who will have the role and who do not already have it
        var conditions = new List<string>();
    
        for (int i = 0; i < usersNotInRoleAndWillGetIt.Length; i++)
        {
            conditions.Add($"c.userName=@addUserName{i} AND NOT ARRAY_CONTAINS(c.roles, @role)");
            parameters.Add(($"@addUserName{i}", usersNotInRoleAndWillGetIt[i]));
        }
    
        sb.Append($" AND ({string.Join(" OR ", conditions)})");
        
        parameters.Add(($"@role", roleName));

        var queryDefinition = new QueryDefinition(sb.ToString());
        foreach (var param in parameters)
        {
            queryDefinition.WithParameter(param.name, param.value);
        }
        return queryDefinition;
        
    }
    
    
    private QueryDefinition BuildQueryDefinitionForUsersToRemoveFromRole(string roleName, string[] usersAlreadyInRoleAndWillHaveItRemoved)
    {
        var sb = new StringBuilder("SELECT c.id, c.userName, c.emailAddress, c.roles,c.type,c.versionNumber,c.firstName,c.lastName,c.profilePhotographSmall,c.profilePhotographOriginal,c.theme,c.createdAt,c.updatedAt,c.lastLoginAt,c.lastPasswordChangeAt FROM c WHERE 1=1");
        var parameters = new List<(string name, object value)>();
        
        var conditions = new List<string>();
    
        // users who are in the role and will have it removed
        for (int i = 0; i < usersAlreadyInRoleAndWillHaveItRemoved.Length; i++)
        {
            conditions.Add($"c.userName=@removeUserName{i} AND NOT ARRAY_CONTAINS(c.roles, @role)");
            parameters.Add(($"@removeUserName{i}", usersAlreadyInRoleAndWillHaveItRemoved[i]));
        }
    
        sb.Append($" AND ({string.Join(" OR ", conditions)})");
        
        parameters.Add(($"@role", roleName));

        var queryDefinition = new QueryDefinition(sb.ToString());
        foreach (var param in parameters)
        {
            queryDefinition.WithParameter(param.name, param.value);
        }
        return queryDefinition;
        
    }

    
    /// <inheritdoc />
    /// <exception cref="ItemCreationException">Thrown if the creation failed.</exception>
    /// <exception cref="ItemAlreadyExistsException">Thrown if the role already exists.</exception>
    /// <exception cref="ArgumentException">Thrown if the Role name is <c>null</c> or whitespace.</exception>
    public async Task<Role> CreateRoleAsync(string roleName, string? description)
    {
        if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException(nameof(roleName));

        // verify role doesn't already exist
        var existingRole = await GetRoleByNameAsync(roleName);
        if (existingRole != null)
        {
            throw new ItemAlreadyExistsException(typeof(Role));
        }

        var newRole = new Role()
        {
            Id = Guid.NewGuid(),
            RoleName = roleName,
            Description = description,
            SchemaVersionNumber = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Roles);
        var response = await container.CreateItemAsync(newRole, new PartitionKey(roleName));

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new ItemCreationException(typeof(Role), response.StatusCode);
        }
        
        return newRole;
    }
    



    private QueryDefinition BuildQueryDefinitionForGetRoles(GetRolesCriteria criteria)
    {
        var sb = new StringBuilder(@"SELECT c.id, c.roleName, c.description, c.type, c.schemaVersionNumber, c.createdAt, c.updatedAt 
                                   FROM c 
                                   WHERE 1=1");
        var parameters = new List<(string name, object value)>();
        
        if (criteria.AnyOfRoleNames!=null && criteria.AnyOfRoleNames.Any())
        {
            var conditions = new List<string>();
            var rolesList = criteria.AnyOfRoleNames.ToList();

            for (int i = 0; i < rolesList.Count; i++)
            {
                conditions.Add($"c.roleName = @role{i}");
                parameters.Add(($"@role{i}", rolesList[i]));
            }

            sb.Append($" AND ({string.Join(" OR ", conditions)})");
        }

        if (!string.IsNullOrWhiteSpace(criteria.ContainingText))
        {
            sb.Append(@" AND (
                            CONTAINS(UPPER(c.name), @containsText) OR 
                            CONTAINS(UPPER(c.description), @containsText)
                            )");
            parameters.Add(("@containsText", criteria.ContainingText.ToUpperInvariant()));
        }
        
        sb.Append(" ORDER BY c.name, c.id");
        
        var queryDefinition = new QueryDefinition(sb.ToString());
        foreach (var param in parameters)
        {
            queryDefinition.WithParameter(param.name, param.value);
        }
        
        
        return queryDefinition;
    }

    private static ContainerProperties CreateRolesContainerProperties()
    {
        var containerProperties = new ContainerProperties(ContainerNames.Roles, ContainerNames.RoleNamePartitionKey)
        {
            IndexingPolicy = new IndexingPolicy()
        };

        containerProperties.IndexingPolicy.CompositeIndexes.Add(new Collection<CompositePath>
        {
            new()
            {
                Path = "/name",
                Order = CompositePathSortOrder.Ascending
            },
            new()
            {
                Path = "/id",
                Order = CompositePathSortOrder.Ascending
            }
        });

        return containerProperties;
    }
    
    

}
