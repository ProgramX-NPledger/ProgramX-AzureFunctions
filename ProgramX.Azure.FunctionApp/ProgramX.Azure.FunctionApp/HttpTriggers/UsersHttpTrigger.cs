using System.Net;
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
        CosmosClient cosmosClient)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;

        _container = _cosmosClient.GetContainer("core", "users");

        
    }


    
    [Function(nameof(GetUser))]
    public async Task<HttpResponseBase> GetUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/{id?}")] HttpRequestData httpRequestData,
        string? id)
    {
        return await RequiresAuthentication(httpRequestData, null, async () =>
        {
            // pass a filter into the below
            var pagedAndFilteredCosmosDbReader =
                new PagedCosmosDBReader<SecureUser>(_cosmosClient, "core", "users");
            
            var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]);
            QueryDefinition queryDefinition;
            if (id == null)
            {
                queryDefinition = new QueryDefinition("SELECT * FROM c order by c.userName");
            }
            else
            {
                queryDefinition = new QueryDefinition("SELECT * FROM c where c.id=@id");
                queryDefinition.WithParameter("@id", id);
            }
            var users = await pagedAndFilteredCosmosDbReader.GetItems(queryDefinition,continuationToken,DataConstants.ItemsPerPage);
            
            if (id==null)
            {
                return new GetUsersHttpResponse(httpRequestData, users.Items,users.ContinuationToken);
            }
            else
            {
                var user = users.Items.FirstOrDefault(q=>q.Id==id);
                if (user == null) return new NotFoundHttpResponse(httpRequestData,"User");

                List<Application> applications = user.Roles.SelectMany(q=>q.Applications).GroupBy(g=>g.Name).Select(q=>q.First()).ToList();
                
             
                return new GetUserHttpResponse(httpRequestData,user,applications);
                
            }
        });
    }
    
    // editing a user will also add/edit roles and applications
    // it is not possible to create a role or application because they each have a parent
    
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
            var response = await _container.CreateItemAsync(httpResponseData.User, new PartitionKey(httpResponseData.User.UserName));
        
            if (response.StatusCode == HttpStatusCode.Created) return httpResponseData;
        
            return new ServerErrorHttpResponse(httpRequestData, "Failed to create user");
        });
     }
    
    
    
}