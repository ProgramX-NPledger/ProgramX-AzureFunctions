using System.Net;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
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
 
    
    [Function(nameof(GetRole))]
    public async Task<HttpResponseBase> GetRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "role/{name?}")] HttpRequestData httpRequestData,
        string? name)
    {
        return await RequiresAuthentication(httpRequestData, null, async () =>
        {
            // pass a filter into the below
            var rolesPagedCosmosDbReader =
                new PagedCosmosDBReader<Role>(_cosmosClient, "core", "users");
            
            var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]);
            QueryDefinition queryDefinition;
            if (name == null)
            {
                queryDefinition = new QueryDefinition("SELECT r.name, r.description, r.applications FROM c JOIN r IN c.roles");
            }
            else
            {
                queryDefinition = new QueryDefinition("SELECT r.name, r.description, r.applications FROM c JOIN r IN c.roles where r.name=@name");
                queryDefinition.WithParameter("@name", name);
            }
            var roles = await rolesPagedCosmosDbReader.GetItems(queryDefinition,continuationToken,DataConstants.ItemsPerPage);
            
            if (name==null)
            {
                return new GetRolesHttpResponse(httpRequestData, roles.Items.OrderBy(q=>q.Name),roles.ContinuationToken);
            }
            else
            {
                var role = roles.Items.FirstOrDefault(q=>q.Name==name);
                if (role == null) return new NotFoundHttpResponse(httpRequestData,"Role");

                var usersCosmosDbReader = new PagedCosmosDBReader<User>(_cosmosClient, "core", "users");
                var users = await usersCosmosDbReader.GetItems(new QueryDefinition("SELECT * FROM u"),null,null);
                var distinctUsers = users.Items.GroupBy(q=>q.id).Select(q=>new SecureUser()
                {
                    id = q.First().id,
                    userName = q.First().userName,
                    emailAddress = q.First().emailAddress,
                    roles = q.First().roles,
                }).ToList();
                var usersInRole = users.Items.GroupBy(q=>q.id)
                    .Where(q=>q.First().roles.Select(q=>q.Name).Contains(role.Name))
                    .Select(q=>new UserInRole()
                {
                    Id = q.First().id,
                    UserName = q.First().userName,
                    EmailAddress = q.First().emailAddress,
                }).ToList();
                
                List<Application> applications = distinctUsers.SelectMany(q=>q.roles)
                        .SelectMany(q=>q.Applications)
                        .GroupBy(g=>g.Name)
                        .Select(q=>q.First()).ToList();
                
             
                return new GetRoleHttpResponse(httpRequestData,role,applications,distinctUsers,usersInRole);
                
            }
        });
    }

    // [Function(nameof(CreateRole))]
    // public async Task<HttpResponseBase> CreateRole(
    //     [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "role")]
    //     HttpRequestData httpRequestData
    // )
    // {
    //     return await RequiresAuthentication(httpRequestData, null,  async () =>
    //     {
    //         var createRoleRequest = await httpRequestData.ReadFromJsonAsync<CreateRoleRequest>();
    //         if (createRoleRequest==null) return new BadRequestHttpResponse(httpRequestData, "Invalid request body");
    //
    //         var httpResponseData = new CreateRoleHttpResponse(httpRequestData, createRoleRequest);
    //         var response = await _container.CreateItemAsync(httpResponseData.Role, new PartitionKey(httpResponseData.Role.Name));
    //     
    //         if (response.StatusCode == HttpStatusCode.Created) return httpResponseData;
    //     
    //         return new ServerErrorHttpResponse(httpRequestData, "Failed to create role");
    //     });
    //  }
    
    
    
}