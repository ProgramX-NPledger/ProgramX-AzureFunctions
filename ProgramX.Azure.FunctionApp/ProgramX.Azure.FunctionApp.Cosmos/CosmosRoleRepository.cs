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
    /// <exception cref="RepositoryException">Thrown if the deletion failed.</exception>
    public async Task DeleteRoleByNameAsync(string roleName)
    {
        using (logger.BeginScope("DeleteRoleByNameAsync {roleName}", roleName))
        {
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException(nameof(roleName));

            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Roles);
            var response = await container.DeleteItemAsync<UserPassword>(roleName, new PartitionKey(roleName));

            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                logger.LogError(
                    "Failed to delete Role with name {roleName} with status code {statusCode} and response {response}", roleName,
                    response.StatusCode, response);
                throw new RepositoryException(OperationType.Delete, typeof(UserPassword));
            }
        }
    }

    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the update failed.</exception>
    public async Task UpdateRoleAsync(Role role)
    {
        using (logger.BeginScope("UpdateRoleAsync {role}", role))
        {
            // it is not possible to change the name of the role, because that is used as the partition key
            
            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);
            var response = await container.ReplaceItemAsync(role, role.RoleName, new PartitionKey(role.RoleName));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError(
                    "Failed to update Role with id {roleName} with status code {statusCode} and response {response}", role.RoleName,
                    response.StatusCode, response);
                throw new RepositoryException(OperationType.Update, typeof(User));
            }
            logger.LogDebug("Success");
        }
    }

    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the creation failed.</exception>
    /// <remarks>
    /// This will not set the user's password. Instead, this is performed when the user sets their password as a separate operation.
    /// </remarks>
    public async Task CreateRoleAsync(Role role)
    {
        using (logger.BeginScope("CreateRoleAsync {role}", role))
        {
            // populate Cosmos-specific fields
            role.Id = Guid.NewGuid();
            role.SchemaVersionNumber = 2;
            role.CreatedAt = DateTime.UtcNow;
            role.UpdatedAt = DateTime.UtcNow;
            
            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Roles);
            var response = await container.CreateItemAsync(role, new PartitionKey(role.RoleName));

            if (response.StatusCode != HttpStatusCode.Created)
            {
                logger.LogError(
                    "Failed to create Role with name {roleName} with status code {statusCode} and response {response}", role.RoleName,
                    response.StatusCode, response);
                throw new RepositoryException(OperationType.Create, typeof(UserPassword));
            }
            logger.LogDebug("Success");
        }    
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