using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

/// <summary>
/// Base class for authorised HTTP triggers.
/// </summary>
public abstract class AuthorisedHttpTriggerBase 
{
    private readonly IConfiguration _configuration;
    public const string AuthenticationHeaderName = "Authorization";

    /// <summary>
    /// Configuration API.
    /// </summary>
    public IConfiguration Configuration
    {
        get => _configuration;
        init => _configuration = value;
    }

    /// <summary>
    /// Authentication information. If this is <c>null</c>, the user is not authenticated.
    /// </summary>
    protected AuthenticationInfo? Authentication { get; private set; }

    /// <summary>
    /// Called by derived class constructor to perform initialisation.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    protected AuthorisedHttpTriggerBase(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected async Task<HttpResponseData> RequiresAuthentication(HttpRequestData httpRequestData, string? requiredRole, Func<string?,IEnumerable<string>?,Task<HttpResponseData>> httpResponseDelegate, bool permitAnonymous=false)
    {
        if (!permitAnonymous)
        {
            if (!httpRequestData.Headers.Contains(AuthenticationHeaderName))
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,
                    "No authentication header was found.");
            }

            var authorisationHeader = httpRequestData.Headers.GetValues(AuthenticationHeaderName);
            var safeAuthorisationHeader = authorisationHeader.First();
            if (safeAuthorisationHeader.StartsWith("Bearer "))
                safeAuthorisationHeader = safeAuthorisationHeader.Substring(7);

            var jwtKey = _configuration["JwtKey"];
            try
            {
                Authentication = new AuthenticationInfo(safeAuthorisationHeader, jwtKey);
            }
            catch (Exception exception)
            {
                return await HttpResponseDataFactory.CreateForServerError(httpRequestData, exception);
            }

            if (!Authentication.IsValid)
            {
                // this should redirect
                return await HttpResponseDataFactory.CreateForUnauthorised(httpRequestData);
            }
            
            return await httpResponseDelegate.Invoke(Authentication.Username, Authentication.Roles);
        }

        return await httpResponseDelegate.Invoke(null,null);
    }

}