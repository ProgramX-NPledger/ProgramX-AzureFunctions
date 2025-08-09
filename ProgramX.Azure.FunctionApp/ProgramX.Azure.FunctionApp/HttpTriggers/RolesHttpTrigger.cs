using System.Net;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class RolesHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<UsersHttpTrigger> _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

 
    public RolesHttpTrigger(ILogger<UsersHttpTrigger> logger,
        CosmosClient cosmosClient)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;

        _container = _cosmosClient.GetContainer("core", "roles");

        
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

    [Function(nameof(CreateRole))]
    public async Task<HttpResponseBase> CreateRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "role")]
        HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, null,  async () =>
        {
            var createRoleRequest = await httpRequestData.ReadFromJsonAsync<CreateRoleRequest>();
            if (createRoleRequest==null) return new BadRequestHttpResponse(httpRequestData, "Invalid request body");

            var httpResponseData = new CreateRoleHttpResponse(httpRequestData, createRoleRequest);
            var response = await _container.CreateItemAsync(httpResponseData.Role, new PartitionKey(httpResponseData.Role.Name));
        
            if (response.StatusCode == HttpStatusCode.Created) return httpResponseData;
        
            return new ServerErrorHttpResponse(httpRequestData, "Failed to create role");
        });
     }
    
    
    
}