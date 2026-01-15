using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

/// <summary>
/// Base class for authorised HTTP triggers.
/// </summary>
public abstract class AuthorisedHttpTriggerBase 
{
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;
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
    protected AuthorisedHttpTriggerBase(IConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected async Task<HttpResponseData> RequiresAuthentication(HttpRequestData httpRequestData, IEnumerable<string>? requiredAnyOfRoles, Func<string?,IEnumerable<string>?,Task<HttpResponseData>> httpResponseDelegate, bool permitAnonymous=false)
    {
        if (!permitAnonymous)
        {
            if (!httpRequestData.Headers.Contains(AuthenticationHeaderName))
            {
                _logger.LogWarning("No authentication header was found.");
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,
                    "No authentication header was found.");
            }

            var authorisationHeader = httpRequestData.Headers.GetValues(AuthenticationHeaderName);
            var safeAuthorisationHeader = authorisationHeader.First();
            if (safeAuthorisationHeader.StartsWith("Bearer "))
                safeAuthorisationHeader = safeAuthorisationHeader.Substring(7);

            var jwtKey = _configuration["JwtKey"];
            if (string.IsNullOrWhiteSpace(jwtKey)) return await HttpResponseDataFactory.CreateForServerError(httpRequestData, "JwtKey not found in configuration.");
            
            try
            {
                Authentication = new AuthenticationInfo(safeAuthorisationHeader, jwtKey);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to parse JWT token.");
                return await HttpResponseDataFactory.CreateForServerError(httpRequestData, exception);
            }

            if (!Authentication.IsValid)
            {
                // this should redirect
                _logger.LogWarning("Invalid JWT token for user {username}", Authentication.Username);
                return await HttpResponseDataFactory.CreateForUnauthorised(httpRequestData);
            }
            
            // check roles membership
            if (requiredAnyOfRoles != null)
            {
                requiredAnyOfRoles =
                    requiredAnyOfRoles.Where(q =>
                        !string.IsNullOrWhiteSpace(q)).ToArray(); // remove blanks and avoid multiple enumerations
                if (!Authentication.Roles.Any(requiredAnyOfRoles.Contains))
                {
                    // does not have required role
                    _logger.LogWarning("User {username} does not have required role(s) {roles}",
                        Authentication.Username, string.Join(",", requiredAnyOfRoles));
                    return await HttpResponseDataFactory.CreateForUnauthorised(httpRequestData);
                }

            }

            return await httpResponseDelegate.Invoke(Authentication.Username, Authentication.Roles);
        }
        
        return await httpResponseDelegate.Invoke(null,null);
    }

}