using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class UsersHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<UsersHttpTrigger> _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

 
    public UsersHttpTrigger(ILogger<UsersHttpTrigger> logger,
        CosmosClient cosmosClient, IConfiguration configuration) : base(configuration)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;

        _container = _cosmosClient.GetContainer("core", "users");

        
    }


    
    [Function(nameof(GetUser))]
    public async Task<HttpResponseData> GetUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/{id?}")] HttpRequestData httpRequestData,
        string? id)
    {
        return await RequiresAuthentication(httpRequestData, null, async (userName, _) =>
        {
            // pass a filter into the below
            var pagedAndFilteredCosmosDbReader =
                new PagedCosmosDBReader<SecureUser>(_cosmosClient, "core", "users");
            
            var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]);
            QueryDefinition queryDefinition;
            if (string.IsNullOrWhiteSpace(id))
            {
                queryDefinition = new QueryDefinition("SELECT * FROM c order by c.userName");
            }
            else
            {
                queryDefinition = new QueryDefinition("SELECT * FROM c where c.id=@id or c.userName=@id");
                queryDefinition.WithParameter("@id", id);
            }
            var users = await pagedAndFilteredCosmosDbReader.GetItems(queryDefinition,continuationToken,DataConstants.ItemsPerPage);
            
            if (string.IsNullOrWhiteSpace(id))
            {
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<SecureUser>()
                {
                    ContinuationToken = users.ContinuationToken,
                    Items = users.Items,
                    IsLastPage = !users.IsMorePages(),
                    ItemsPerPage = users.MaximumItemsRequested
                });
            }
            else
            {
                var user = users.Items.FirstOrDefault(q=>q.id==id || q.userName==id);
                if (user == null)
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
                }

                if (!userName.Equals(user.userName))
                {
                    return await HttpResponseDataFactory.CreateForUnauthorised(httpRequestData);
                }
                
                List<Application> applications = user.roles.SelectMany(q=>q.Applications).GroupBy(g=>g.Name).Select(q=>q.First()).ToList();
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
                {
                    user,
                    applications,
                    profilePhotoBase64 = string.Empty
                });
                
                
            }
        });
    }

    [Function(nameof(UpdateUser))]
    public async Task<HttpResponseData> UpdateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "user/{id}")]
        HttpRequestData httpRequestData,
        string id)
    {
        return await RequiresAuthentication(httpRequestData, null,  async (_, _) =>
        {
            var updateUserRequest = await httpRequestData.ReadFromJsonAsync<UpdateUserRequest>();
            if (updateUserRequest==null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");

            // get the User to get the password hash
            var pagedAndFilteredCosmosDbReader =
                new PagedCosmosDBReader<User>(_cosmosClient, "core", "users");

            QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.id=@id OR c.userName=@id");
            queryDefinition.WithParameter("@id", id);
            var users = await pagedAndFilteredCosmosDbReader.GetItems(queryDefinition);
            var originalUser = users.Items.FirstOrDefault();
            if (originalUser == null)
            {
                return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
            }
            
            if (originalUser.userName!=updateUserRequest.userName) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Cannot change the username because it is used for the Partition Key");
            
            originalUser.emailAddress=updateUserRequest.emailAddress;
            originalUser.roles=updateUserRequest.roles;
            
            var response = await _container.ReplaceItemAsync(originalUser, id, new PartitionKey(updateUserRequest.userName));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData);
            }
        
            return await HttpResponseDataFactory.CreateForServerError(httpRequestData, "Failed to update user");
        });
    }
    
    [Function(nameof(CreateUser))]
    public async Task<HttpResponseData> CreateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user")] HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, null,  async (_, _) =>
        {
            var createUserRequest = await httpRequestData.ReadFromJsonAsync<CreateUserRequest>();
            if (createUserRequest==null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid request body");

            var rolesCosmosDbReader =
                new PagedCosmosDBReader<Role>(_cosmosClient, "core", "users");
            
            var queryDefinition= new QueryDefinition("SELECT r.name, r.description, r.applications FROM c JOIN r IN c.roles");
            var roles = await rolesCosmosDbReader.GetItems(queryDefinition,null,null);
            
            using var hmac = new HMACSHA512();
            var allRoles = roles.Items.ToList(); // avoid multiple enumeration
        
            var newUser = new User()
            {
                id = Guid.NewGuid().ToString("N"),
                emailAddress = createUserRequest.EmailAddress,
                userName = createUserRequest.UserName,
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(createUserRequest.Password)),
                passwordSalt = hmac.Key,
                roles = allRoles.Where(q=>createUserRequest.AddToRoles.Select(r=>r.Name).Contains(q.Name))
                    .Union(
                        createUserRequest.AddToRoles
                            .Where(q=>!allRoles
                                .Select(r=>r.Name)
                                .Contains(q.Name)
                            )
                            .Select(q=>new Role()
                            {
                                Name = q.Name,
                                Description = q.Description,
                                Applications = q.Applications
                            }))
                    .ToList()
            };
        
            var response = await _container.CreateItemAsync(newUser, new PartitionKey(newUser.userName));;

            if (response.StatusCode == HttpStatusCode.Created)
            {
                // redact the password
                newUser.passwordHash = new byte[0];
                newUser.passwordSalt = new byte[0];
                
                return await HttpResponseDataFactory.CreateForCreated(httpRequestData, newUser, "user", newUser.id);
                
            }
        
            return await HttpResponseDataFactory.CreateForServerError(httpRequestData, "Failed to create user");
        });
     }
    
    
    
}