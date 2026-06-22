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

            result.IsRequiredToBeOrderedByClient = false;
            return result;
        }
    }


    /// <inheritdoc />
    public IEnumerable<User> GetUsersInRole(string roleName, IEnumerable<User> users)
    {
        // TODO return all users with role name in roles
        return Enumerable.Empty<User>();
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


    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown if the user name is null or whitespace.</exception>
    /// <exception cref="RepositoryException">Thrown if the update failed.</exception>
    public async Task<User> UpdateUserAsync(string userName, string emailAddress, string? firstName, string? lastName, IEnumerable<string> roles)
    {
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException(nameof(userName));

        var existingUser = await GetUserByUserNameAsync(userName);
        if (existingUser == null)
        {
            throw new ItemNotFoundException(OperationType.Update, typeof(User), "User does not exist");
        }

        // it is not possible to change the name of the user, because that is used as the partition key
        if (existingUser.UserName != userName)
        {
            throw new UpdateImmutablePropertyException(OperationType.Update, typeof(User), "Attempt to change User name prevented. User Name is immutable.", "RoleName");           
        }
        
        existingUser.EmailAddress = emailAddress;
        existingUser.FirstName = firstName;
        existingUser.LastName = lastName;
        existingUser.Roles = roles.ToList();
        
        var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);
        var response = await container.ReplaceItemAsync(existingUser, existingUser.UserName, new PartitionKey(userName));

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new ItemUpdateException(typeof(Role), response.StatusCode);
        }
        
        return existingUser;       
        
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown if the user name is null or whitespace.</exception>
    /// <exception cref="RepositoryException">Thrown if the update failed.</exception>
    public async Task<User> UpdateUserSettingsAsync(string userName, string? theme)
    {
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException(nameof(userName));

        var existingUser = await GetUserByUserNameAsync(userName);
        if (existingUser == null)
        {
            throw new ItemNotFoundException(OperationType.Update, typeof(User), "User does not exist");
        }
        
        existingUser.Theme = theme ?? existingUser.Theme;
        
        var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);
        var response = await container.ReplaceItemAsync(existingUser, existingUser.UserName, new PartitionKey(userName));

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new ItemUpdateException(typeof(Role), response.StatusCode);
        }
        
        return existingUser;       
        
    }
    
    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the creation failed.</exception>
    /// <remarks>
    /// This will not set the user's password. Instead, this is performed when the user sets their password as a separate operation.
    /// </remarks>
    public async Task<User> CreateUserAsync(string userName, string emailAddress, string? firstName, string? lastName, IEnumerable<string> roles, DateTime passwordConfirmationLinkExpiryDate)
    {
        var existingUser = await GetUserByUserNameAsync(userName);
        if (existingUser != null) throw new ItemAlreadyExistsException(typeof(User), $"User with name {userName} already exists");
        
        var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);
        
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            EmailAddress = emailAddress,
            FirstName = firstName,
            LastName = lastName,
            Roles = roles,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = null,
            LastPasswordChangeAt = null,
            PasswordConfirmationNonce = Guid.NewGuid().ToString(),
            PasswordLinkExpiresAt = passwordConfirmationLinkExpiryDate,
            ProfilePhotographOriginal = null,
            ProfilePhotographSmall = null,
            SchemaVersionNumber = 6,
            Theme = "light",
            UpdatedAt = DateTime.Now
        };
        
        var response = await container.CreateItemAsync(user, new PartitionKey(user.UserName));

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new ItemCreationException(typeof(User), response.StatusCode);
        }

        return user;
    }

    /// <inheritdoc/>
    /// <exception cref="ItemNotFoundException">Thrown if the user does not exist.</exception>
    /// <exception cref="InvalidPasswordUpdateException">Thrown if the password update is invalid.</exception>
    /// <exception cref="ItemUpdateException">Thrown if the user password update fails.</exception>
    /// <remarks>This assumes the provided password has passed password strength validation.</remarks>
    public async Task<User> UpdateUserPasswordAsync(string userName, string newPassword, string passwordConfirmationNonce)
    {
        var userPasswordsContainer = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.UserPasswords);
        var usersContainer = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);
        
        // verify the nonce, expiration
        var user = await GetUserByUserNameAsync(userName);
        if (user == null) throw new ItemNotFoundException(OperationType.Update, typeof(User), $"User {userName} not found");
        if (user.PasswordConfirmationNonce != passwordConfirmationNonce) throw new InvalidPasswordUpdateException(InvalidPasswordUpdateReason.InvalidConfirmationNonce);
        if (user.PasswordLinkExpiresAt < DateTime.UtcNow) throw new InvalidPasswordUpdateException(InvalidPasswordUpdateReason.PasswordResetLinkExpired);
        
        var userPassword = await GetUserPasswordByUserNameAsync(userName);
        
        ItemResponse<UserPassword> userPasswordResponse;
        if (userPassword == null)
        {
            // password is new
            
            userPassword = CreateNewUserPassword(userName, newPassword);
            userPasswordResponse = await userPasswordsContainer.CreateItemAsync(userPassword, new PartitionKey(userName));
            if (userPasswordResponse.StatusCode != HttpStatusCode.Created)
            {
                throw new ItemUpdateException(typeof(UserPassword), userPasswordResponse.StatusCode);
            }
        }
        else
        {
            userPasswordResponse = await userPasswordsContainer.ReplaceItemAsync(userPassword, userPassword.id, new PartitionKey(userPassword.userName));
            if (userPasswordResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ItemUpdateException(typeof(UserPassword), userPasswordResponse.StatusCode);
            }
        }
        
        // user is changing their password so reset these
        user.PasswordConfirmationNonce = null;
        user.PasswordLinkExpiresAt = null;
            
        ItemResponse<User> userResponse;
        userResponse = await usersContainer.ReplaceItemAsync(user, userName, new PartitionKey(user.UserName));
        if (userResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new ItemUpdateException(typeof(UserPassword), userResponse.StatusCode);
        }

        return user;
    }

    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the update failed.</exception>
    public async Task<User> AddRoleToUserAsync(string roleName, string userName)
    {
        using (logger.BeginScope("AddRoleToUser {role}, {userName}", roleName, userName))
        {
            var container = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);

            var user = await GetUserByUserNameAsync(userName);
            if (user == null)
            {
                logger.LogError("User {userName} not found", userName);
                throw new RepositoryException(OperationType.Update, typeof(User), $"User {userName} not found");
            }

            user.Roles = user.Roles.Union([roleName]).ToList();
            var response = await container.ReplaceItemAsync(user, user.Id, new PartitionKey(user.UserName));
            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError(
                    "Failed to update user with id {id} with status code {statusCode} and response {response}",
                    user.Id, response.StatusCode, response);
                throw new RepositoryException(OperationType.Update, typeof(User));
            }
            logger.LogDebug("Added Role to user {userName}", user.UserName);
            return user;
        }
    }

    /// <inheritdoc />
    /// <exception cref="ItemNotFoundException">Thrown if the User does not have the specified Role.</exception>
    public async Task<User> RemoveRoleFromUserAsync(string roleName, string userName)
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

            if (user.Roles.Any(q => q.Equals(roleName, StringComparison.InvariantCultureIgnoreCase)))
            {
                user.Roles = user.Roles.Where(q => !q.Equals(roleName, StringComparison.InvariantCultureIgnoreCase)).ToList();  
                var response = await container.ReplaceItemAsync(user, user.Id, new PartitionKey(user.UserName));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    logger.LogError(
                        "Failed to update user with UserName {userName} with status code {statusCode} and response {response}",
                        userName, response.StatusCode, response);
                    throw new RepositoryException(OperationType.Update, typeof(User));
                }

                return user;
            }
            else
            {
                // user does not have role
                throw new ItemNotFoundException(OperationType.Update, typeof(Role), $"User {userName} does not have role {roleName}");
            }
        }
    }

    private UserPassword CreateNewUserPassword(string userName, string newPassword)
    {
        using var hmac = new HMACSHA512();
        var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(newPassword));
        var passwordSalt = hmac.Key;                
        var userPassword = new UserPassword()
        {
            userName = userName,
            id = Guid.NewGuid().ToString(),
            passwordHash = passwordHash,
            passwordSalt = passwordSalt,
        };
        return userPassword;
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
                ContainerNames.UserPasswordPartitionKey);
            
            IResult<UserPassword> result = await cosmosReader.GetItemsAsync(queryDefinition);

            if (!result.Items.Any())
            {
                logger.LogDebug("No UserPassword found for user {userName}. Looking in {usersContainer} to verify.", userName, ContainerNames.Users);
                
                queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.userName=@userName");
                queryDefinition.WithParameter("@userName", userName);
            
                logger.LogDebug("QueryDefinition: {queryDefinition}", queryDefinition);
            
                CosmosReader<User> userCosmosReader = new CosmosReader<User>(cosmosClient,
                    DatabaseNames.Core,
                    ContainerNames.Users,
                    ContainerNames.UserNamePartitionKey);
            
                IResult<User> userResult = await userCosmosReader.GetItemsAsync(queryDefinition);

                if (!userResult.Items.Any())
                {
                    logger.LogError("No User found for user {userName}. No UserPassword found.", userName);
                    return null;
                }
                
                logger.LogInformation("User {userName} found in Container {usersPasswordsContainer} but has no password in Container {usersContainer}. Creating UserPassword.", userName, ContainerNames.Users, ContainerNames.UserPasswords);

                var usersContainer = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Users);
                ItemResponse<dynamic> usersResponse = await usersContainer.ReadItemAsync<dynamic>(
                    id: userName, 
                    partitionKey: new PartitionKey("userName")
                );

                var userPasswordHashAsString = usersResponse.Resource.passwordHash;
                var userPasswordSaltAsString = usersResponse.Resource.passwordSalt;
                if (userPasswordHashAsString == null || userPasswordSaltAsString == null)
                {
                    logger.LogError("User {userName} does not have correct schema in source Container {usersContainer} and password cannot be automatically migrated",userName,ContainerNames.Users);
                    return null;
                }
                
                var userPasswordsContainer = cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.UserPasswords);
                var userPasswordsResponse = await userPasswordsContainer.CreateItemAsync(new UserPassword()
                {
                    id = Guid.NewGuid().ToString(),
                    userName = userName,
                    passwordHash = userPasswordHashAsString!,
                    passwordSalt = userPasswordSaltAsString!
                }, new PartitionKey(userName));
                if (userPasswordsResponse.StatusCode != HttpStatusCode.Created)
                {
                    logger.LogError(
                        "Failed to update UserPassword with UserName {userName} with status code {statusCode} and response {response}", userName,
                        usersResponse.StatusCode, usersResponse);
                    throw new RepositoryException(OperationType.Update, typeof(User));
                }
                
                logger.LogInformation("Successfully migrated UserPassword for user {userName}", userName);
                return userPasswordsResponse.Resource;
            }

            result.IsRequiredToBeOrderedByClient = false;
            return result.Items.SingleOrDefault();
        }
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