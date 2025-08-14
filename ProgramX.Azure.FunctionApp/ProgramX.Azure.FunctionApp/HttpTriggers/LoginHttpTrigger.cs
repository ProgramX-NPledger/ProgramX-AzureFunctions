using System.Net;
using System.Security.Cryptography;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class LoginHttpTrigger
{
    private readonly ILogger<LoginHttpTrigger> _logger;
    private readonly JwtTokenIssuer _jwtTokenIssuer;
    private readonly CosmosClient _cosmosClient;

    public LoginHttpTrigger(ILogger<LoginHttpTrigger> logger,
        JwtTokenIssuer jwtTokenIssuer,
        CosmosClient cosmosClient)
    {
        _logger = logger;
        _jwtTokenIssuer = jwtTokenIssuer;
        _cosmosClient = cosmosClient;
    }

    [Function(nameof(Login))]
    public async Task<HttpResponseData> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "login")] HttpRequestData httpRequestData,
        ILogger logger)
    {
        var credentials = await httpRequestData.ReadFromJsonAsync<Credentials>();
        if (credentials == null)
        {
            return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid request body");
        }
        
        var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.userName = @userName");
        queryDefinition.WithParameter("@userName", credentials.UserName);
        var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync("core");
        if (database.StatusCode==HttpStatusCode.Created) _logger.LogInformation("Database created");
        var container = await database.Database.CreateContainerIfNotExistsAsync("users", "/userName");
        if (container.StatusCode==HttpStatusCode.Created) _logger.LogInformation("Container created");
        var users = container.Container.GetItemQueryIterator<ProgramX.Azure.FunctionApp.Model.User>(queryDefinition);
        var user = await users.ReadNextAsync();
        if (user.Count == 0)
        {
            return await HttpResponseDataFactory.CreateForUnauthorised(httpRequestData);
        }
            
        var userFromDb = user.First();
        
        using var hmac = new HMACSHA512(userFromDb.passwordSalt);
        var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(credentials.Password));

        for (var i = 0; i < computedHash.Length; i++)
            if (computedHash[i] != userFromDb.passwordHash[i])
            {
                return await HttpResponseDataFactory.CreateForUnauthorised(httpRequestData);
            }

        string token = _jwtTokenIssuer.IssueTokenForUser(credentials);
 
        var httpResponse = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        await httpResponse.WriteAsJsonAsync(new
        {
            token = token,
        });
        return httpResponse;
        // var httpResponse = new LoginSuccessHttpResponse(httpRequestData,token);
        // return await httpResponse.GetHttpResponseAsync();

    }
}