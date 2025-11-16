using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Cosmos;

public class CosmosUserRepository(CosmosClient cosmosClient, ILogger<CosmosUserRepository> logger) : IUserRepository
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown if required initialisation properties are <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Thrown if required parameters are <c>null</c>.</exception>
    public async Task<IResult<Role>> GetRolesAsync(GetRolesCriteria criteria, PagedCriteria? pagedCriteria = null)
    {
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

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown if required initialisation properties are <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Thrown if required parameters are <c>null</c>.</exception>
    public async Task<IResult<SecureUser>> GetUsersAsync(GetUsersCriteria criteria, PagedCriteria? pagedCriteria = null)
    {
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

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown if required initialisation properties are <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Thrown if required parameters are <c>null</c>.</exception>
    public async Task<IResult<Application>> GetApplicationsAsync(GetApplicationsCriteria criteria,
        PagedCriteria? pagedCriteria = null)
    {
        QueryDefinition queryDefinition = BuildQueryDefinitionForApplications(criteria);

        CosmosReader<Application> cosmosReader;
        IResult<Application> result;
        if (pagedCriteria != null)
        {
            cosmosReader = new CosmosPagedReader<Application>(cosmosClient,
                DataConstants.CoreDatabaseName,
                DataConstants.UsersContainerName,
                DataConstants.UserNamePartitionKeyPath);
            result = await ((CosmosPagedReader<Application>)cosmosReader).GetPagedItemsAsync(queryDefinition,
                pagedCriteria.Offset,
                pagedCriteria.ItemsPerPage);
        }
        else
        {
            cosmosReader = new CosmosReader<Application>(cosmosClient,
                DataConstants.CoreDatabaseName,
                DataConstants.UsersContainerName,
                DataConstants.UserNamePartitionKeyPath);
            result = await cosmosReader.GetItemsAsync(queryDefinition);
        }
        
        // it isn't possible to order within a collection, so we need to get all the items and the caller must sort the results
        result.IsRequiredToBeOrderedByClient = false;
        return result;

    }

    /// <inheritdoc />
    public IEnumerable<SecureUser> GetUsersInRole(string roleName, IEnumerable<SecureUser> users)
    {
        return users.GroupBy(q=>q.id)
            .Where(q=>q.First().roles.Select(q=>q.name).Contains(roleName))
            .SelectMany((g=>g.ToList()));
    }

    /// <inheritdoc />
    public async Task<SecureUser?> GetUserByIdAsync(string id)
    {
        var users = await GetUsersAsync(new GetUsersCriteria()
        {
            Id = id,
        });
        return users.Items.SingleOrDefault();
    }

    /// <inheritdoc />
    public async Task<SecureUser?> GetUserByUserNameAsync(string userName)
    {
        var users = await GetUsersAsync(new GetUsersCriteria()
        {
            UserName = userName
        });
        return users.Items.SingleOrDefault();
    }

    /// <inheritdoc />
    public async Task DeleteUserByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(nameof(id));

        var container = cosmosClient.GetContainer(DataConstants.CoreDatabaseName, DataConstants.UsersContainerName);
        var response = await container.DeleteItemAsync<User>(id, new PartitionKey(id));

        if (response.StatusCode != HttpStatusCode.NoContent)
        {
            logger.LogError("Failed to delete user with id {id}",id,response.StatusCode,response);
            throw new RepositoryException(OperationType.Delete,typeof(User));
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetInsecureUserByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(nameof(id));
        
        QueryDefinition queryDefinition = BuildQueryDefinitionForUsers(new GetUsersCriteria()
        {
            Id = id
        });

        CosmosReader<User> cosmosReader;
        IResult<User> result;

        cosmosReader = new CosmosReader<User>(cosmosClient, 
                DataConstants.CoreDatabaseName, 
                DataConstants.UsersContainerName, 
                DataConstants.UserNamePartitionKeyPath);
        result = await cosmosReader.GetItemsAsync(queryDefinition);
        
        return result.Items.SingleOrDefault();
    }

    /// <inheritdoc />
    public async Task<Application?> GetApplicationByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(name));
        
        var applications = await GetApplicationsAsync(new GetApplicationsCriteria()
        { 
            ApplicationName = name
        });
        return applications.Items.SingleOrDefault();
    }

    /// <inheritdoc />
    public async Task UpdateUserAsync(SecureUser user)
    {
        var container = cosmosClient.GetContainer(DataConstants.CoreDatabaseName, DataConstants.UsersContainerName);
        var response = await container.ReplaceItemAsync(user, user.id, new PartitionKey(user.userName));

        if (response.StatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to update user with id {id}",user.id,response.StatusCode,response);
            throw new RepositoryException(OperationType.Update,typeof(SecureUser));
        }
    }

    /// <inheritdoc />
    public async Task CreateUserAsync(User user)
    {
        var container = cosmosClient.GetContainer(DataConstants.CoreDatabaseName, DataConstants.UsersContainerName);
        var response = await container.CreateItemAsync(user, new PartitionKey(user.userName));

        if (response.StatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to create user",response.StatusCode,response);
            throw new RepositoryException(OperationType.Create,typeof(User));;
        }
    }
    
    
    /// <inheritdoc />
    public async Task CreateRoleAsync(Role role, IEnumerable<string> usersInRoles)
    {
        var container = cosmosClient.GetContainer(DataConstants.CoreDatabaseName, DataConstants.UsersContainerName);
        
        // get all users in the role
        var users = await GetUsersAsync(
            new GetUsersCriteria()
            {
                UserNames = usersInRoles
            });
        if (users.TotalCount == 0)
        {
            throw new RepositoryException(
                $"No users found with usernames so cannot add Role: {string.Join(",", usersInRoles)}");
        }
        
        // add role to each user
        foreach (var user in users.Items)
        {
            ((List<Role>)user.roles).Add(role);
            var response = await container.ReplaceItemAsync(user, user.id, new PartitionKey(user.userName));
            
        }
            
    }

    public async Task CreateApplicationAsync(Application application, IEnumerable<string> withinRoles)
    {
        var container = cosmosClient.GetContainer(DataConstants.CoreDatabaseName, DataConstants.UsersContainerName);
        
        // get all users with roles
        var users = await GetUsersAsync(new GetUsersCriteria()
        {
            WithRoles = withinRoles.ToList()
        });
        if (users.TotalCount == 0) throw new RepositoryException($"No users found with roles so cannot add Application: {application.name}");
        
        // add application to each role to each user
        foreach (var user in users.Items)
        {
            foreach (var role in user.roles)
                if (((List<Application>)role.applications).All(q => q.name != application.name))
                {
                    // add application to role
                    ((List<Application>)role.applications).Add(application);
                }
                else
                {
                    throw new RepositoryException($"Application {application.name} already exists in role {role.name}");
                }

            var response = await container.ReplaceItemAsync(user, user.id,
                new PartitionKey(user.userName));
            if (response.StatusCode != HttpStatusCode.OK)
                throw new RepositoryException(OperationType.Create, typeof(Application));
        }        
    
        
    }

    public async Task<Role?> GetRoleByNameAsync(string name)
    {
        var roles = await GetRolesAsync(new GetRolesCriteria()
        { 
            RoleName = name,
        });
        return roles.Items.SingleOrDefault();
        
    }

    public async Task UpdateRoleAsync(string roleName, Role role)
    {
        var container = cosmosClient.GetContainer(DataConstants.CoreDatabaseName, DataConstants.UsersContainerName);
        
        var allUsersInRole = await GetUsersAsync(new GetUsersCriteria()
        {
            WithRoles = new List<string>() { roleName }
        });
        
        if (allUsersInRole.TotalCount == 0) throw new RepositoryException(OperationType.Update,typeof(Role), $"No users found with role {roleName}");

        foreach (var user in allUsersInRole.Items)
        {
            Role innerRole = user.roles.SingleOrDefault(q=>q.name == roleName);
            if (innerRole != null)
            {
                innerRole.name = role.name;
                innerRole.description = role.description;
                innerRole.applications = role.applications;
                innerRole.updatedAt = role.updatedAt;
            }
            var response = await container.ReplaceItemAsync(user, user.id, new PartitionKey(user.userName));
            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError("Failed to update user with id {id}",user.id,response.StatusCode,response);
                throw new RepositoryException(OperationType.Update,typeof(SecureUser));
            }
        }
    }

    public async Task UpdateApplicationAsync(string applicationName, Application application)
    {
        var container = cosmosClient.GetContainer(DataConstants.CoreDatabaseName, DataConstants.UsersContainerName);
        
        // get all users with roles
        var users = await GetUsersAsync(new GetUsersCriteria()
        {
            HasAccessToApplications = [applicationName]
        });
        
        // update application in each role in each user
        foreach (var user in users.Items)
        {
            foreach (var role in user.roles)
            {
                var existingApplication = role.applications.SingleOrDefault(q=>q.name == applicationName);
                if (existingApplication != null)
                {
                    existingApplication.name = application.name;
                    existingApplication.description = application.description;
                    existingApplication.imageUrl = application.imageUrl;
                    existingApplication.targetUrl = application.targetUrl;
                    existingApplication.schemaVersionNumber = application.schemaVersionNumber;
                    existingApplication.isDefaultApplicationOnLogin = application.isDefaultApplicationOnLogin;
                    existingApplication.ordinal = application.ordinal;
                    existingApplication.updatedAt = DateTime.Now;
                }
            }
            
            var response = await container.ReplaceItemAsync(user, user.id,
                new PartitionKey(user.userName));
            if (response.StatusCode != HttpStatusCode.OK)
                throw new RepositoryException(OperationType.Update, typeof(Application));
        }        

    }

    public async Task DeleteRoleByNameAsync(string roleName)
    {
        var container = cosmosClient.GetContainer(DataConstants.CoreDatabaseName, DataConstants.UsersContainerName);
        
        var allUsersInRole = await GetUsersAsync(new GetUsersCriteria()
        {
            WithRoles = new List<string>() { roleName }
        });

        foreach (var user in allUsersInRole.Items)
        {
            user.roles = user.roles.Where(q => !q.name.Equals(roleName, StringComparison.InvariantCultureIgnoreCase)).ToList();
            var response = await container.ReplaceItemAsync(user, user.id, new PartitionKey(user.userName));
            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError("Failed to update user with id {id}",user.id,response.StatusCode,response);
                throw new RepositoryException(OperationType.Update,typeof(SecureUser));
            }
        }

    }

    public async Task DeleteApplicationByNameAsync(string applicationName)
    {
        var container = cosmosClient.GetContainer(DataConstants.CoreDatabaseName, DataConstants.UsersContainerName);
        
        // get all users with roles
        var users = await GetUsersAsync(new GetUsersCriteria()
        {
            HasAccessToApplications = [applicationName]
        });
        
        // remove application from each role from each user
        foreach (var user in users.Items)
        {
            foreach (var role in user.roles)
            {
                role.applications = role.applications.Where(q => !q.name.Equals(applicationName, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }
            
            var response = await container.ReplaceItemAsync(user, user.id,
                new PartitionKey(user.userName));
            if (response.StatusCode != HttpStatusCode.OK)
                throw new RepositoryException(OperationType.Delete, typeof(Application));
        }        
        
        
    }

    private QueryDefinition BuildQueryDefinitionForApplications(GetApplicationsCriteria criteria)
    {
        var sb = new StringBuilder("SELECT a.name, a.description, a.imageUrl, a.targetUrl, a.type, a.schemaVersionNumber, a.isDefaultApplicationOnLogin, a.ordinal, a.createdAt,a.updatedAt FROM c JOIN r IN c.roles JOIN a IN r.applications WHERE 1=1");
        var parameters = new List<(string name, object value)>();
        if (!string.IsNullOrWhiteSpace(criteria.ApplicationName))
        {
            sb.Append(" AND (a.name=@id)");
            parameters.Add(("@id", criteria.ApplicationName));
        }

        if (!string.IsNullOrWhiteSpace(criteria.ContainingText))
        {
            sb.Append(@" AND (
                            CONTAINS(UPPER(a.name), @containsText) OR 
                            CONTAINS(UPPER(a.description), @containsText)
                            )");
            parameters.Add(("@containsText", criteria.ContainingText.ToUpperInvariant()));
        }

        if (criteria.WithinRoles != null && criteria.WithinRoles.Any())
        {
            var conditions = new List<string>();
            var rolesList = criteria.WithinRoles.ToList();
            
            for (int i = 0; i < rolesList.Count; i++)
            {
                conditions.Add($"EXISTS(SELECT VALUE rr FROM rr IN c.roles WHERE rr.name = @role{i})");
                parameters.Add(($"@role{i}", rolesList[i]));
            }
            
            sb.Append($" AND ({string.Join(" OR ", conditions)})");
        }

        if (criteria.ApplicationNames != null && criteria.ApplicationNames.Any())
        {
            var conditions = new List<string>();
            var applicationsList = criteria.ApplicationNames.ToList();
        
            for (int i = 0; i < applicationsList.Count; i++)
            {
                conditions.Add(@$"EXISTS(SELECT VALUE a FROM a IN c.roles JOIN a IN r.applications WHERE a.name = @appname{i})");
                parameters.Add(($"@appname{i}", applicationsList[i]));
            }
        
            sb.Append($" AND ({string.Join(" OR ", conditions)})");
        }
        
        sb.Append(" GROUP BY a.name, a.description, a.imageUrl, a.targetUrl, a.type, a.schemaVersionNumber, a.isDefaultApplicationOnLogin, a.ordinal, a.createdAt,a.updatedAt");
        
        var queryDefinition = new QueryDefinition(sb.ToString());
        foreach (var param in parameters)
        {
            queryDefinition.WithParameter(param.name, param.value);
        }
        return queryDefinition;
    }



    private QueryDefinition BuildQueryDefinitionForRoles(GetRolesCriteria criteria)
    {
        var sb = new StringBuilder(@"SELECT r.name, r.description, r.applications, r.type, r.schemaVersionNumber, r.createdAt,r.updatedAt, ARRAY(SELECT VALUE a FROM a IN r.applications) 
                                   FROM c 
                                   JOIN r IN c.roles 
                                   WHERE 1=1");
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

            sb.Append(" AND (1=0 OR EXISTS(SELECT VALUE a FROM a IN r.applications WHERE (1=0");
            for (int i = 0; i < applicationsList.Count; i++)
            {
                sb.Append($" OR a.name=@appname{0})");
                parameters.Add(($"@appname{i}", applicationsList[i]));
            }
            sb.Append("))");
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
        
        if (criteria.UserNames != null && criteria.UserNames.Any())
        {
            var conditions = new List<string>();
            var usersList = criteria.UserNames.ToList();
        
            for (int i = 0; i < usersList.Count; i++)
            {
                conditions.Add(@$"EXISTS(SELECT VALUE c FROM c WHERE c.userName = @username{i})");
                parameters.Add(($"@username{i}", usersList[i]));
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