using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Osm.Model;

namespace ProgramX.Azure.FunctionApp.Osm;

public class AuthTokenHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;
    private readonly IIntegrationRepository _integrationRepository;
    private readonly ILogger<AuthTokenHandler> _logger;

    public const string OsmServiceName = "osm";
    
    // In a real app, inject a service that manages token storage/refreshing
    private string? _bearerToken = null;
    private string? _refreshToken = null;

    public AuthTokenHandler(IConfiguration configuration, IIntegrationRepository integrationRepository, ILogger<AuthTokenHandler> logger)
    {
        _configuration = configuration;
        _integrationRepository = integrationRepository;
        _logger = logger;

        // the BearerToken may be immediately invalid, but this will be handled
        // by using the RefreshToken to get a new one
    }


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // if we do not have any cached tokens, try get get them from persisted storage first, then configuration
        if (string.IsNullOrEmpty(_bearerToken) || string.IsNullOrEmpty(_refreshToken))
        {
            var integrationCredentials = await _integrationRepository.GetIntegrationCredentialsForServiceAsync(OsmServiceName);
            if (integrationCredentials == null)
            {
                _bearerToken = _configuration["Osm:BearerToken"];
                _refreshToken = _configuration["Osm:RefreshToken"];
            }
            
            // if the tokens are still not set, we have a problem
            if (string.IsNullOrEmpty(_bearerToken) || string.IsNullOrEmpty(_refreshToken))
            {
                throw new InvalidOperationException("Bearer token or refresh token not found in repository or configuration.");
            }
        }
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            // unauthorised, so try refreshing the token
            if (await RefreshTokensAsync())
            {
                // update the request headers with the new token
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                
                // Dispose the old response before retrying
                response.Dispose();
                response = await base.SendAsync(request, cancellationToken);
            }
        }

        return response;
    }

    private async Task<bool> RefreshTokensAsync()
    {
        var clientId = _configuration["Osm:ClientId"];
        var clientSecret = _configuration["Osm:ClientSecret"];
        
        if (string.IsNullOrEmpty(_refreshToken) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            _logger.LogWarning("Refresh token or client credentials are missing, cannot refresh tokens");
            return false;
        }

        using var refreshClient = new HttpClient();
        var postData = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = _refreshToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        };

        var response = await refreshClient.PostAsync("https://www.onlinescoutmanager.co.uk/oauth/token", new FormUrlEncodedContent(postData));

        if (response.IsSuccessStatusCode)
        {
            var osmTokenRefreshResponse = await response.Content.ReadFromJsonAsync<OsmTokenRefreshResponse>();
            if (osmTokenRefreshResponse == null)
            {
                throw new InvalidOperationException("Could not deserialize token refresh response");
            }
            
            await _integrationRepository.SetBearerAndRefreshTokensAsync(OsmServiceName, clientId, osmTokenRefreshResponse.AccessToken, osmTokenRefreshResponse.RefreshToken);
            
            // update the cached versions
            _bearerToken = osmTokenRefreshResponse.AccessToken;
            _refreshToken = osmTokenRefreshResponse.RefreshToken;
            
            return true;
        }

        return false;
    }
}