using System.Text;
using Microsoft.Azure.Cosmos;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Cosmos;

public class UserRepository(CosmosClient cosmosClient) : IUserRepository
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown if required initialisation properties are <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Thrown if required parameters are <c>null</c>.</exception>
    public async Task<IResult<Role>> GetRolesAsync(GetRolesCriteria criteria, PagedCriteria? pagedCriteria = null)
    {
        if (cosmosClient == null) throw new InvalidOperationException("CosmosDB client is not set");
        if (criteria == null) throw new ArgumentNullException(nameof(criteria));
        
        QueryDefinition queryDefinition = BuildQueryDefinitionForRoles(criteria);

        CosmosReader<Role> cosmosReader;
        IResult<Role> result;
        if (pagedCriteria != null)
        {
            cosmosReader  = new CosmosPagedReader<Role>(cosmosClient, 
                DataConstants.CoreDatabaseName, 
                DataConstants.UsersContainerName, 
                DataConstants.UserNamePartitionKeyPath);
            result = await ((CosmosPagedReader<Role>)cosmosReader).GetPagedItemsAsync(queryDefinition, pagedCriteria.Offset,
                pagedCriteria.ItemsPerPage);
        }
        else
        {
            cosmosReader = new CosmosReader<Role>(cosmosClient, 
                DataConstants.CoreDatabaseName, 
                DataConstants.UsersContainerName, 
                DataConstants.UserNamePartitionKeyPath);
            result = await cosmosReader.GetItemsAsync(queryDefinition);
        }
        
        // it isn't possible to order within a collection, so we need to get all the items and the caller must sort the results
        result.IsRequiredToBeOrderedByClient = true;
        return result;
    }

    public async Task<IResult<SecureUser>> GetUsersAsync(GetUsersCriteria criteria, PagedCriteria? pagedCriteria = null)
    {
        if (cosmosClient == null) throw new InvalidOperationException("CosmosDB client is not set");
        if (criteria == null) throw new ArgumentNullException(nameof(criteria));
        
        QueryDefinition queryDefinition = BuildQueryDefinitionForUsers(criteria);

        CosmosReader<SecureUser> cosmosReader;
        IResult<SecureUser> result;
        if (pagedCriteria != null)
        {
            cosmosReader  = new CosmosPagedReader<SecureUser>(cosmosClient, 
                DataConstants.CoreDatabaseName, 
                DataConstants.UsersContainerName, 
                DataConstants.UserNamePartitionKeyPath);
            result = await ((CosmosPagedReader<SecureUser>)cosmosReader).GetPagedItemsAsync(queryDefinition, pagedCriteria.Offset,
                pagedCriteria.ItemsPerPage);
        }
        else
        {
            cosmosReader = new CosmosReader<SecureUser>(cosmosClient, 
                DataConstants.CoreDatabaseName, 
                DataConstants.UsersContainerName, 
                DataConstants.UserNamePartitionKeyPath);
            result = await cosmosReader.GetItemsAsync(queryDefinition);
        }
        
        result.IsRequiredToBeOrderedByClient = false;
        return result;
        
    }


    private QueryDefinition BuildQueryDefinitionForRoles(GetRolesCriteria criteria)
    {
        var sb = new StringBuilder("SELECT r.name, r.description, r.applications, r.type, r.schemaVersionNumber, r.createdAt,r.updatedAt FROM c JOIN r IN c.roles JOIN a IN r.applications WHERE 1=1");
        var parameters = new List<(string name, object value)>();
        if (!string.IsNullOrWhiteSpace(criteria.RoleName))
        {
            sb.Append(" AND (r.name=@id)");
            parameters.Add(("@id", criteria.RoleName));
        }

        if (!string.IsNullOrWhiteSpace(criteria.ContainingText))
        {
            sb.Append(@" AND (
                            CONTAINS(UPPER(r.name), @containsText) OR 
                            CONTAINS(UPPER(r.description), @containsText)
                            )");
            parameters.Add(("@containsText", criteria.ContainingText.ToUpperInvariant()));
        }

        if (criteria.UsedInApplicationNames != null && criteria.UsedInApplicationNames.Any())
        {
            var applicationsList = criteria.UsedInApplicationNames.ToList();

            for (int i = 0; i < applicationsList.Count; i++)
            {
                parameters.Add(($"@appname{i}", applicationsList[i]));
            }

            sb.Append($" AND a.name IN ({string.Join(",", 
                parameters
                    .Where(q=>q.name.StartsWith("@appname"))
                    .Select(s => s.name)
                )})");
        }

        sb.Append(" GROUP BY r.name, r.description, r.applications, r.type, r.schemaVersionNumber, r.createdAt,r.updatedAt");
        
        var queryDefinition = new QueryDefinition(sb.ToString());
        foreach (var param in parameters)
        {
            queryDefinition.WithParameter(param.name, param.value);
        }
        return queryDefinition;
    }
    
    
    private QueryDefinition BuildQueryDefinitionForUsers(GetUsersCriteria criteria)
    {
        var sb = new StringBuilder("SELECT c.id, c.userName, c.emailAddress, c.roles,c.type,c.versionNumber,c.firstName,c.lastName,c.profilePhotographSmall,c.profilePhotographOriginal,c.theme,c.createdAt,c.updatedAt,c.lastLoginAt,c.lastPasswordChangeAt FROM c WHERE 1=1");
        var parameters = new List<(string name, object value)>();
        
        if (!string.IsNullOrWhiteSpace(criteria.Id))
        {
            sb.Append(" AND (c.id=@id)");
            parameters.Add(("@id", criteria.Id));
        }

        if (!string.IsNullOrWhiteSpace(criteria.UserName))
        {
            sb.Append(" AND (c.userName=@id)");
            parameters.Add(("@id", criteria.UserName));
        }
        
        if (!string.IsNullOrWhiteSpace(criteria.ContainingText))
        {
            sb.Append(@" AND (
                            CONTAINS(UPPER(c.userName), @containsText) OR 
                            CONTAINS(UPPER(c.emailAddress), @containsText) OR 
                            CONTAINS(UPPER(c.firstName), @containsText) OR 
                            CONTAINS(UPPER(c.lastName), @containsText)
                            )");
            parameters.Add(("@containsText", criteria.ContainingText.ToUpperInvariant()));
        }
        
        if (criteria.WithRoles != null && criteria.WithRoles.Any())
        {
            var conditions = new List<string>();
            var rolesList = criteria.WithRoles.ToList();
        
            for (int i = 0; i < rolesList.Count; i++)
            {
                conditions.Add($"EXISTS(SELECT VALUE r FROM r IN c.roles WHERE r.name = @role{i})");
                parameters.Add(($"@role{i}", rolesList[i]));
            }
        
            sb.Append($" AND ({string.Join(" OR ", conditions)})");
        }

        if (criteria.HasAccessToApplications != null && criteria.HasAccessToApplications.Any())
        {
            var conditions = new List<string>();
            var applicationsList = criteria.HasAccessToApplications.ToList();
        
            for (int i = 0; i < applicationsList.Count; i++)
            {
                conditions.Add(@$"EXISTS(SELECT VALUE r FROM r IN c.roles JOIN a IN r.applications WHERE a.name = @appname{i})");
                parameters.Add(($"@appname{i}", applicationsList[i]));
            }
        
            sb.Append($" AND ({string.Join(" OR ", conditions)})");
        }

        var queryDefinition = new QueryDefinition(sb.ToString());
        foreach (var param in parameters)
        {
            queryDefinition.WithParameter(param.name, param.value);
        }
        return queryDefinition;
        
    }

}