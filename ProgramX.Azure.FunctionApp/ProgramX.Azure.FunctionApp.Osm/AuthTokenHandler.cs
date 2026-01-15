using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace ProgramX.Azure.FunctionApp.Osm;

public class AuthTokenHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;

    // In a real app, inject a service that manages token storage/refreshing
    private string _bearerToken = "your_initial_token";
    private readonly string _refreshToken = "your_refresh_token";

    public AuthTokenHandler(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // the BearerToken may be immediately invalid, but this will be handled
        // by using the RefreshToken to get a new one
        _bearerToken = _configuration["Osm:BearerToken"];
        _refreshToken = _configuration["Osm:RefreshToken"];
    }


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Attach the current token
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

        var response = await base.SendAsync(request, cancellationToken);

        // 2. If unauthorized, try to refresh
        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            if (await RefreshTokensAsync())
            {
                // 3. Retry the request with the new token
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
        // Implement your actual refresh logic here calling the external identity provider
        // e.g., var newTokens = await identityClient.Refresh(_refreshToken);
        _bearerToken = "new_refreshed_token"; 
        return true;
    }
}