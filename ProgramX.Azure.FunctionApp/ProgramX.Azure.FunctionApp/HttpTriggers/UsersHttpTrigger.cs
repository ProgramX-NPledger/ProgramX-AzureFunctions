using System.Net;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
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
        CosmosClient cosmosClient)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;

        _container = _cosmosClient.GetContainer("core", "users");

        
    }
    //
    // [Function(nameof(GetUsers))]
    // public async Task<HttpResponseBase> GetUsers(
    //     [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user")] HttpRequestData httpRequestData,
    //     [CosmosDBInput("core","user",Connection = "CosmosDBConnection", SqlQuery = "SELECT * FROM c order by c.userName")] IEnumerable<User> users)
    // {
    //     return RequiresAuthentication(httpRequestData, null, () =>
    //     {
    //         return new GetUsersHttpResponse(httpRequestData,users);
    //     });
    //     
    //     // https://charliedigital.com/2020/05/24/azure-functions-with-jwt-authentication/
    //     
    //     return: roles, applications
    //
    // }

    
    [Function(nameof(GetUser))]
    public async Task<HttpResponseBase> GetUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/{id?}")] HttpRequestData httpRequestData,
        string? id)
    {
        return await RequiresAuthentication(httpRequestData, null, async () =>
        {
            var users = CosmosDBUtility.GetItems<User>( _cosmosClient, "core","users",new QueryDefinition("SELECT * FROM c"));
            if (id==null)
            {
                return new GetUsersHttpResponse(httpRequestData, users);
            }
            else
            {
                var user = users.FirstOrDefault(q=>q.id==id);
                if (user == null) return new NotFoundHttpResponse(httpRequestData,"User");

                user.passwordHash = [];
                user.passwordSalt = [];
                // return user details, application, profile photo
            
                // get all roles
                var allRoles = CosmosDBUtility.GetItems<Role>( _cosmosClient, "core","roles",new QueryDefinition("SELECT * FROM c ORDER BY c.name"));
                var allPermittedApplications = CosmosDBUtility.GetItems<Application>( _cosmosClient, "core","applications",new QueryDefinition("SELECT * FROM c ORDER BY c.name")).Where(q=>allRoles.Select(qq=>qq.applicationId).Contains(q.id));
                var allPermittedRoles = allRoles; // TODO: Intersect
                return new GetUserHttpResponse(httpRequestData,user,allPermittedApplications, allPermittedRoles.Select(q=>q.name));
                
            }
        });
    }
    
  
    
    [Function(nameof(CreateUser))]
    public async Task<HttpResponseBase> CreateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user")]
        HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, null,  async () =>
        {
            var createUserRequest = await httpRequestData.ReadFromJsonAsync<CreateUserRequest>();
            if (createUserRequest==null) return new BadRequestHttpResponse(httpRequestData, "Invalid request body");

            var httpResponseData = new CreateUserHttpResponse(httpRequestData, createUserRequest);
            var response = await _container.CreateItemAsync(httpResponseData.User, new PartitionKey(httpResponseData.User.userName));
        
            if (response.StatusCode == HttpStatusCode.Created) return httpResponseData;
        
            return new ServerErrorHttpResponse(httpRequestData, "Failed to create user");
        });
     }
    
    
    
}