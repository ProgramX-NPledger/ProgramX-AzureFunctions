using System.Net;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class LoginHttpTrigger
{
    private readonly ILogger<LoginHttpTrigger> _logger;
    private readonly IJwtAlgorithm _algorithm;
    private readonly IJsonSerializer _serializer;
    private readonly IBase64UrlEncoder _base64Encoder;
    private readonly IJwtEncoder _jwtEncoder;
    
    public LoginHttpTrigger(ILogger<LoginHttpTrigger> logger)
    {
        _algorithm = new HMACSHA256Algorithm();
        _serializer = new JsonNetSerializer();
        _base64Encoder = new JwtBase64UrlEncoder();
        _jwtEncoder = new JwtEncoder(_algorithm, _serializer, _base64Encoder);
        _logger = logger;
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
        
        // Instead of returning a string, we'll return the JWT with a set of claims about the user
        Dictionary<string, object> claims = new Dictionary<string, object>
        {
            // JSON representation of the user Reference with ID and display name
            { "username", credentials.UserName },

            // TODO: Add other claims here as necessary; maybe from a user database
            {
                "roles", new[]
                {
                    "admin"
                }
            }
        };
        
        string token = _jwtEncoder.Encode(claims, "YOUR_SECRETY_KEY_JUST_A_LONG_STRING"); // Put this key in config
 
        return new LoginSuccessHttpResponse(httpRequestData, token);

    }
}