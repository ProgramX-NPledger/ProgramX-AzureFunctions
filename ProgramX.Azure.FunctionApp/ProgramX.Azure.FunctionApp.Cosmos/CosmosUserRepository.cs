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

public class CosmosUserRepository(CosmosClient cosmosClient, ILogger<CosmosUserRepository> logger) : IUserRepository
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown if required initialisation properties are <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Thrown if required parameters are <c>null</c>.</exception>
    public async Task<IResult<Role>> GetRolesAsync(GetRolesCriteria criteria, PagedCriteria? pagedCriteria = null)
    {
        using (logger.BeginScope("GetRolesAsync {criteria}, {pagedCriteria}", criteria, pagedCriteria?.ToString() ?? "null"))
        {
            QueryDefinition queryDefinition = BuildQueryDefinitionForRoles(criteria);
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
            
            // it isn't possible to order within a collection, so we need to get all the items and the caller must sort the results
            result.IsRequiredToBeOrderedByClient = true;
            return result;
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown if required initialisation properties are <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Thrown if required parameters are <c>null</c>.</exception>
    public async Task<IResult<User>> GetUsersAsync(GetUsersCriteria criteria, PagedCriteria? pagedCriteria = null)
    {
        using (logger.BeginScope("GetUsersAsync {criteria}, {pagedCriteria}", criteria, pagedCriteria?.ToString() ?? "null"))
        {
            QueryDefinition queryDefinition = BuildQueryDefinitionForUsers(criteria);
            logger.LogDebug("QueryDefinition: {queryDefinition}", queryDefinition);
            
            CosmosReader<User> cosmosReader;
            IResult<User> result;
            if (pagedCriteria != null)
            {
                cosmosReader = new CosmosPagedReader<User>(cosmosClient,
                    DatabaseNames.Core,
                    ContainerNames.Users,
                    ContainerNames.UserNamePartitionKey);
                result = await ((CosmosPagedReader<User>)cosmosReader).GetPagedItemsAsync(queryDefinition,
                    pagedCriteria.Offset,
                    pagedCriteria.ItemsPerPage);
            }
            else
            {
                cosmosReader = new CosmosReader<User>(cosmosClient,
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
    /// <exception cref="InvalidOperationException">Thrown if required initialisation properties are <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Thrown if required parameters are <c>null</c>.</exception>
    public async Task<IResult<Application>> GetApplicationsAsync(GetApplicationsCriteria criteria,
        PagedCriteria? pagedCriteria = null)
    {
        using (logger.BeginScope("GetApplicationsAsync {criteria}, {pagedCriteria}", criteria, pagedCriteria?.ToString() ?? "null"))
        {
            QueryDefinition queryDefinition = BuildQueryDefinitionForApplications(criteria);
            logger.LogDebug("QueryDefinition: {queryDefinition}", queryDefinition);
            
            CosmosReader<Application> cosmosReader;
            IResult<Application> result;
            if (pagedCriteria != null)
            {
                cosmosReader = new CosmosPagedReader<Application>(cosmosClient,
                    DatabaseNames.Core,
                    ContainerNames.Users,
                    ContainerNames.UserNamePartitionKey);
                result = await ((CosmosPagedReader<Application>)cosmosReader).GetPagedItemsAsync(queryDefinition,
                    pagedCriteria.Offset,
                    pagedCriteria.ItemsPerPage);
            }
            else
            {
                cosmosReader = new CosmosReader<Application>(cosmosClient,
                    DatabaseNames.Core,
                    ContainerNames.Users,
                    ContainerNames.UserNamePartitionKey);
                result = await cosmosReader.GetItemsAsync(queryDefinition);
            }
            logger.LogDebug("Result: {result}", result);
            
            // it isn't possible to order within a collection, so we need to get all the items and the caller must sort the results
            result.IsRequiredToBeOrderedByClient = false;
            return result;
        }

    }

    /// <inheritdoc />
    public IEnumerable<User> GetUsersInRole(string roleName, IEnumerable<User> users)
    {
        return users.GroupBy(q=>q.id)
            .Where(q=>q.First().roles.Select(q=>q.name).Contains(roleName))
            .SelectMany((g=>g.ToList()));
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(nameof(id));
        var users = await GetUsersAsync(new GetUsersCriteria()
        {
            Id = id,
        });
        return users.Items.SingleOrDefault();
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByUserNameAsync(string userName)
    {
        var users = await GetUsersAsync(new GetUsersCriteria()
        {
            UserName = userName
        });
        return users.Items.SingleOrDefault();
    }

    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the deletion failed.</exception>
    public async Task DeleteUserByIdAsync(string id)
    {
        using (logger.BeginScope("DeleteUserByIdAsync {id}", id))
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(nameof(id));

            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);
            var response = await container.DeleteItemAsync<UserPassword>(id, new PartitionKey(id));

            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                logger.LogError(
                    "Failed to delete user with id {id} with status code {statusCode} and response {response}", id,
                    response.StatusCode, response);
                throw new RepositoryException(OperationType.Delete, typeof(UserPassword));
            }
        }
    }

    // /// <inheritdoc />
    // public async Task<UserPassword?> GetInsecureUserByIdAsync(string id)
    // {
    //     using (logger.BeginScope("GetInsecureUserByIdAsync {id}", id))
    //     {
    //         if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(nameof(id));
    //
    //         QueryDefinition queryDefinition = BuildQueryDefinitionForUsers(new GetUsersCriteria()
    //         {
    //             Id = id
    //         });
    //         logger.LogDebug("QueryDefinition: {queryDefinition}", queryDefinition);
    //
    //         CosmosReader<UserPassword> cosmosReader;
    //         IResult<UserPassword> result;
    //
    //         cosmosReader = new CosmosReader<UserPassword>(cosmosClient,
    //             DatabaseNames.Core,
    //             ContainerNames.Users,
    //             ContainerNames.UserNamePartitionKey);
    //         result = await cosmosReader.GetItemsAsync(queryDefinition);
    //         logger.LogDebug("Success but logging inhibited because of sensitive data");
    //
    //         return result.Items.SingleOrDefault();
    //     }
    // }

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
    /// <exception cref="RepositoryException">Thrown if the update failed.</exception>
    public async Task UpdateUserAsync(User user)
    {
        using (logger.BeginScope("UpdateUserAsync {user}", user))
        {
            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);
            var response = await container.ReplaceItemAsync(user, user.id, new PartitionKey(user.userName));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError(
                    "Failed to update user with id {id} with status code {statusCode} and response {response}", user.id,
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
    public async Task CreateUserAsync(User user)
    {
        using (logger.BeginScope("CreateUserAsync {user}", user))
        {
            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);
            var response = await container.CreateItemAsync(user, new PartitionKey(user.userName));

            if (response.StatusCode != HttpStatusCode.Created)
            {
                logger.LogError(
                    "Failed to create user with id {id} with status code {statusCode} and response {response}", user.id,
                    response.StatusCode, response);
                throw new RepositoryException(OperationType.Create, typeof(UserPassword));
            }
            logger.LogDebug("Success");
        }    
    }
    
    
    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the creation failed.</exception>
    public async Task CreateRoleAsync(Role role, IEnumerable<string> usersInRoles)
    {
        using (logger.BeginScope("CreateRoleAsync {role}, {usersInRoles}", role, usersInRoles))
        {
            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);
            usersInRoles = usersInRoles.ToList(); // avoid multiple enumeration

            // get all users in the role
            var users = await GetUsersAsync(
                new GetUsersCriteria()
                {
                    UserNames = usersInRoles
                });
            if (users.TotalCount == 0)
            {
                logger.LogError("No users found with usernames {usersInRoles} so cannot add Role. At least one User must exist for Role to be created.",string.Join(",", usersInRoles));
                throw new RepositoryException(
                    $"No users found with usernames so cannot add Role: {string.Join(",", usersInRoles)}");
            }

            // add role to each user
            foreach (var user in users.Items)
            {
                ((List<Role>)user.roles).Add(role);
                await container.ReplaceItemAsync(user, user.id, new PartitionKey(user.userName));
                logger.LogDebug("Added Role to User: {userName}", user.userName);           
            }
        }
            
    }

    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the creation failed.</exception>
    public async Task CreateApplicationAsync(Application application, IEnumerable<string> withinRoles)
    {
        using (logger.BeginScope("CreateApplicationAsync {application}, {withinRoles}", application, withinRoles))
        {
            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);

            withinRoles = withinRoles.ToList(); // avoid multiple enumerations
            
            // get all users with roles
            var users = await GetUsersAsync(new GetUsersCriteria()
            {
                WithRoles = withinRoles.ToList()
            });
            if (users.TotalCount == 0)
            {
                logger.LogError("No users found with roles {withinRoles} so cannot add Application. At least one User must exist with one of the Roles for the Application to be created.",string.Join(",", withinRoles));
                throw new RepositoryException(
                    $"No users found with roles so cannot add Application: {application.name}");
            }

            // add application to each role to each user
            foreach (var user in users.Items)
            {
                foreach (var role in user.roles)
                    if (((List<Application>)role.applications).All(q => q.name != application.name))
                    {
                        // add application to role
                        logger.LogDebug("Adding Application {applicationName} to Role {roleName}", application.name, role.name);
                        ((List<Application>)role.applications).Add(application);
                    }
                    else
                    {
                        logger.LogError("Application {applicationName} already exists in role {roleName}", application.name, role.name);
                        throw new RepositoryException(
                            $"Application {application.name} already exists in role {role.name}");
                    }

                var response = await container.ReplaceItemAsync(user, user.id,
                    new PartitionKey(user.userName));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    logger.LogError("Failed to update user with id {id} with status code {statusCode} and response {response}",user.id,response.StatusCode,response);
                    throw new RepositoryException(OperationType.Create, typeof(Application));
                }
                logger.LogDebug("Added Application to User: {userName}", user.userName);           
            }
            logger.LogDebug("Success");
        }  
    
        
    }

    /// <inheritdoc />
    public async Task<Role?> GetRoleByNameAsync(string name)
    {
        var roles = await GetRolesAsync(new GetRolesCriteria()
        { 
            RoleName = name,
        });
        return roles.Items.SingleOrDefault();
        
    }

    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the update failed.</exception>
    public async Task UpdateRoleAsync(string roleName, Role role)
    {
        using (logger.BeginScope("UpdateRoleAsync {roleName}, {role}", roleName, role))
        {
            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);

            var allUsersInRole = await GetUsersAsync(new GetUsersCriteria()
            {
                WithRoles = new List<string>() { roleName }
            });

            if (allUsersInRole.TotalCount == 0)
            {
                logger.LogError("No users found with role {roleName}. At least one User must have the Role defined.", roleName);
                throw new RepositoryException(OperationType.Update, typeof(Role),
                    $"No users found with role {roleName}");
            }

            foreach (var user in allUsersInRole.Items)
            {
                logger.LogDebug("Updating Role {roleName} for User {userName}", roleName, user.userName);
                Role? innerRole = user.roles.SingleOrDefault(q => q.name == roleName);
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
                    logger.LogError(
                        "Failed to update user with id {id} with status code {statusCode} and response {response}",
                        user.id, response.StatusCode, response);
                    throw new RepositoryException(OperationType.Update, typeof(User));
                }
                logger.LogDebug("Success");
            }
            logger.LogDebug("Success");
        }
    }

    /// <inheritdoc />
    public async Task UpdateApplicationAsync(string applicationName, Application application)
    {
        var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);
        
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
                    existingApplication.schemaVersionNumber = (application.schemaVersionNumber <= 2) ? 2 : application.schemaVersionNumber;
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

    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the deletion failed.</exception>
    public async Task DeleteRoleByNameAsync(string roleName)
    {
        using (logger.BeginScope("DeleteRoleByNameAsync {roleName}", roleName))
        {
            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);

            var allUsersInRole = await GetUsersAsync(new GetUsersCriteria()
            {
                WithRoles = new List<string>() { roleName }
            });
            logger.LogDebug("Users with Role: {allUsersInRole}", allUsersInRole);
            
            foreach (var user in allUsersInRole.Items)
            {
                user.roles = user.roles
                    .Where(q => !q.name.Equals(roleName, StringComparison.InvariantCultureIgnoreCase)).ToList();
                var response = await container.ReplaceItemAsync(user, user.id, new PartitionKey(user.userName));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    logger.LogError(
                        "Failed to update user with id {id} with status code {statusCode} and response {response}",
                        user.id, response.StatusCode, response);
                    throw new RepositoryException(OperationType.Update, typeof(User));
                }
                logger.LogDebug("Removed Role from user {userName}", user.userName);                      
            }
        }

    }

    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the deletion failed.</exception>
    public async Task DeleteApplicationByNameAsync(string applicationName)
    {
        using (logger.BeginScope("DeleteApplicationByNameAsync {applicationName}", applicationName))
        {
            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);

            // get all users with roles
            var users = await GetUsersAsync(new GetUsersCriteria()
            {
                HasAccessToApplications = [applicationName]
            });
            logger.LogDebug("Users with Application: {users}", users);

            // remove Application from each role from each user
            foreach (var user in users.Items)
            {
                foreach (var role in user.roles)
                {
                    role.applications = role.applications.Where(q =>
                        !q.name.Equals(applicationName, StringComparison.InvariantCultureIgnoreCase)).ToList();
                }

                var response = await container.ReplaceItemAsync(user, user.id,
                    new PartitionKey(user.userName));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new RepositoryException(OperationType.Delete, typeof(Application));
                }
                logger.LogDebug("Removed Application from user {userName}", user.userName);           
            }
        }
        
    }

    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the update failed.</exception>
    public async Task AddRoleToUserAsync(Role role, string userName)
    {
        using (logger.BeginScope("AddRoleToUser {role}, {userName}", role, userName))
        {
            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);

            var user = await GetUserByUserNameAsync(userName);
            if (user == null)
            {
                logger.LogError("User {userName} not found", userName);
                throw new RepositoryException(OperationType.Update, typeof(User), $"User {userName} not found");
            }

            user.roles = user.roles.Union([role]).ToList();
            var response = await container.ReplaceItemAsync(user, user.id, new PartitionKey(user.userName));
            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError(
                    "Failed to update user with id {id} with status code {statusCode} and response {response}",
                    user.id, response.StatusCode, response);
                throw new RepositoryException(OperationType.Update, typeof(User));
            }
            logger.LogDebug("Added Role to user {userName}", user.userName);                      
        }
    }

    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the update failed.</exception>
    public async Task RemoveRoleFromUserAsync(string roleName, string userName)
    {
        using (logger.BeginScope("RemoveRoleFromUser {roleName}, {userName}", roleName, userName))
        {
            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);

            var user = await GetUserByUserNameAsync(userName);
            if (user == null)
            {
                logger.LogError("User {userName} not found", userName);
                throw new RepositoryException(OperationType.Update, typeof(User), $"User {userName} not found");
            }

            if (user.roles.Any(q => q.name.Equals(roleName, StringComparison.InvariantCultureIgnoreCase)))
            {
                user.roles = user.roles.Where(q => !q.name.Equals(roleName, StringComparison.InvariantCultureIgnoreCase)).ToList();  
                
                var response = await container.ReplaceItemAsync(user, user.id, new PartitionKey(user.userName));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    logger.LogError(
                        "Failed to update user with id {id} with status code {statusCode} and response {response}",
                        user.id, response.StatusCode, response);
                    throw new RepositoryException(OperationType.Update, typeof(User));
                }
                logger.LogDebug("Removed Role from user {userName}", user.userName);  
            }
            else
            {
                logger.LogWarning("User {userName} does not have Role {roleName}. No action taken.", userName, roleName);
            }
            
                              
        }
    }

    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the update fails.</exception>
    public async Task UpdateUserPasswordAsync(string userName, string newPassword)
    {
        using (logger.BeginScope("UpdateUserPasswordAsync {userName}", userName))
        {
            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.UserPasswordsPartitionKey);

            var userPassword = await GetUserPasswordByUserNameAsync(userName);
            ItemResponse<UserPassword> response;
            if (userPassword == null)
            {
                // password is new
                
                using var hmac = new HMACSHA512();
                var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(newPassword));
                var passwordSalt = hmac.Key;                
                userPassword = new UserPassword()
                {
                    userName = userName,
                    id = Guid.NewGuid().ToString(),
                    passwordHash = passwordHash,
                    passwordSalt = passwordSalt,
                };
                response = await container.CreateItemAsync(userPassword, new PartitionKey(userName));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    logger.LogError(
                        "Failed to update UserPassword with id {id} with status code {statusCode} and response {response}", userPassword.id,
                        response.StatusCode, response);
                    throw new RepositoryException(OperationType.Update, typeof(User));
                }
            }
            else
            {
                response = await container.ReplaceItemAsync(userPassword, userPassword.id, new PartitionKey(userPassword.userName));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    logger.LogError(
                        "Failed to update UserPassword with id {id} with status code {statusCode} and response {response}", userPassword.id,
                        response.StatusCode, response);
                    throw new RepositoryException(OperationType.Update, typeof(User));
                }
            }

            if (response.StatusCode == HttpStatusCode.Created) logger.LogDebug("Created");
            else if (response.StatusCode == HttpStatusCode.OK) logger.LogDebug("Success");
        }
        
    }

    /// <inheritdoc />
    public async Task<UserPassword?> GetUserPasswordByUserNameAsync(string userName)
    {
        using (logger.BeginScope("GetUserPasswordByUserNameAsync {username}", userName))
        {
            QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.userName=@userName");
            queryDefinition.WithParameter("@userName", userName);
            
            logger.LogDebug("QueryDefinition: {queryDefinition}", queryDefinition);
            
            CosmosReader<UserPassword> cosmosReader = new CosmosReader<UserPassword>(cosmosClient,
                DatabaseNames.Core,
                ContainerNames.UserPasswords,
                ContainerNames.UserPasswordsPartitionKey);
            
            IResult<UserPassword> result = await cosmosReader.GetItemsAsync(queryDefinition);

            logger.LogDebug("Result: {result}", result);
            if (!result.Items.Any()) return null;            
            
            result.IsRequiredToBeOrderedByClient = false;
            return result.Items.SingleOrDefault();
        }
    }

    private QueryDefinition BuildQueryDefinitionForApplications(GetApplicationsCriteria criteria)
    {
        var sb = new StringBuilder("SELECT a.name, a.metaDataDotNetAssembly, a.metaDataDotNetType, a.type, a.schemaVersionNumber, a.isDefaultApplicationOnLogin, a.ordinal, a.createdAt,a.updatedAt FROM c JOIN r IN c.roles JOIN a IN r.applications WHERE 1=1");
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
        
        sb.Append(" GROUP BY a.name, a.metaDataDotNetAssembly, a.metaDataDotNetType, a.type, a.schemaVersionNumber, a.isDefaultApplicationOnLogin, a.ordinal, a.createdAt,a.updatedAt");
        
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