using System.Net;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
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
    
    public LoginHttpTrigger(ILogger<LoginHttpTrigger> logger,
        JwtTokenIssuer jwtTokenIssuer)
    {
        _logger = logger;
        _jwtTokenIssuer = jwtTokenIssuer;
    }

    [Function(nameof(Login))]
    public async Task<HttpResponseBase> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "login")] HttpRequestData httpRequestData,
        ILogger logger)
    {
        // https://charliedigital.com/2020/05/24/azure-functions-with-jwt-authentication/
        
        var credentials = await httpRequestData.ReadFromJsonAsync<Credentials>();
        if (credentials == null)
        {
            var invalidCredentialsResponse = new BadRequestHttpResponse(httpRequestData, "Invalid request body");
            return invalidCredentialsResponse;
            // var createCatalogueResponse = new CreateCatalogueResponse()
            // {
            //     HttpResponse = httpRequestData.CreateResponse(HttpStatusCode.BadRequest),
            //     NewCatalogue = null
            // };
            // createCatalogueResponse.HttpResponse.WriteString("Invalid request body");
            // return createCatalogueResponse;
        }
        
        bool isAuthenticated = true; // TODO: something real
        
        if (!isAuthenticated)
        {
            return new InvalidCredentialsOrUnauthorisedHttpResponse(httpRequestData);
        }


        string token = _jwtTokenIssuer.IssueTokenForUser(credentials);
 
        return new LoginSuccessHttpResponse(httpRequestData, token);

    }
}