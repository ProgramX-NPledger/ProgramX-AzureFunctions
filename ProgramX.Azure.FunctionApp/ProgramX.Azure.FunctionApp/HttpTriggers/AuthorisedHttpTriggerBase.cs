using System.Security.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public abstract class AuthorisedHttpTriggerBase 
{
    private readonly IConfiguration _configuration;
    private const string AuthenticationHeaderName = "Authorization";

    public IConfiguration Configuration => _configuration;
    
    // Access the authentication info.
    protected AuthenticationInfo Auth { get; private set; }

    public AuthorisedHttpTriggerBase(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HttpResponseData> RequiresAuthentication(HttpRequestData httpRequestData, string? requiredRole, Func<string,IEnumerable<string>,Task<HttpResponseData>> httpResponseDelegate)
    {
        if (!httpRequestData.Headers.Contains(AuthenticationHeaderName))
        {
            return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "No authentication header was found.");
        }

        var authorisationHeader = httpRequestData.Headers.GetValues(AuthenticationHeaderName);
        var safeAuthorisationHeader = authorisationHeader.First();
        if (safeAuthorisationHeader.StartsWith("Bearer ")) safeAuthorisationHeader = safeAuthorisationHeader.Substring(7);
        
        try
        {
            Auth = new AuthenticationInfo(safeAuthorisationHeader,_configuration["JwtKey"]);
        }
        catch (Exception exception)
        {
            return await HttpResponseDataFactory.CreateForServerError(httpRequestData, exception);
        }

        if (!Auth.IsValid)
        {
            // this should redirect
            return await HttpResponseDataFactory.CreateForUnauthorised(httpRequestData);
        }

        return await httpResponseDelegate.Invoke(Auth.Username, Auth.Roles);
    }

}