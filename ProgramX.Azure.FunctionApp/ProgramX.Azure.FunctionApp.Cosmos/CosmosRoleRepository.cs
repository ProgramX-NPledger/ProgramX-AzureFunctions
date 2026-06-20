using System.Net;
using System.Security.Cryptography;
using System.Text;
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
            if (pagedCriteria != null)
            {
                cosmosReader = new CosmosPagedReader<Role>(cosmosClient,
                    DatabaseNames.Core,
                    ContainerNames.Users,
                    ContainerNames.UserNamePartitionKey);
                result = await ((CosmosPagedReader<Role>)cosmosReader).GetPagedItemsAsync(queryDefinition,
                    pagedCriteria.Offset,
                    pagedCriteria.ItemsPerPage);
            }
            else
            {
                cosmosReader = new CosmosReader<Role>(cosmosClient,
                    DatabaseNames.Core,
                    ContainerNames.Users,
                    ContainerNames.UserNamePartitionKey);
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

        var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Roles);
        var response = await container.DeleteItemAsync<UserPassword>(roleName, new PartitionKey(roleName));

        if (response.StatusCode != HttpStatusCode.NoContent)
        {
            throw new ItemDeleteException(typeof(Role), response.StatusCode);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ItemNotFoundException">Thrown if the Role does not exist.</exception>
    /// <exception cref="UpdateImmutablePropertyException">Thrown if the Role name is attempted to be changed.</exception>
    /// <exception cref="ItemUpdateException">Thrown if the update failed.</exception>   
    /// <exception cref="ArgumentException">Thrown if the Role name is <c>null</c> or whitespace.</exception>
    public async Task<Role> UpdateRoleAsync(string roleName, string? description)
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
        
        var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);
        var response = await container.ReplaceItemAsync(existingRole, existingRole.RoleName, new PartitionKey(roleName));

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new ItemUpdateException(typeof(Role), response.StatusCode);
        }
        
        return existingRole;       
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
        var sb = new StringBuilder(@"SELECT c.id, c.name, c.description, c.type, c.schemaVersionNumber, c.createdAt, c.updatedAt 
                                   FROM c 
                                   WHERE 1=1");
        var parameters = new List<(string name, object value)>();
        
        if (criteria.AnyOfRoleNames!=null && criteria.AnyOfRoleNames.Any())
        {
            sb.Append(" AND (");

            var conditions = new List<string>();
            var rolesList = criteria.AnyOfRoleNames.ToList();

            for (int i = 0; i < criteria.AnyOfRoleNames.Count(); i++)
            {
                if (i > 0) conditions.Add(" OR ");
                conditions.Add($"c.name = @role{i}");
                parameters.Add(($"@role{i}", rolesList[i]));
            }

            sb.Append(")");
        }

        if (!string.IsNullOrWhiteSpace(criteria.ContainingText))
        {
            sb.Append(@" AND (
                            CONTAINS(UPPER(c.roleName), @containsText) OR 
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
    
    

}