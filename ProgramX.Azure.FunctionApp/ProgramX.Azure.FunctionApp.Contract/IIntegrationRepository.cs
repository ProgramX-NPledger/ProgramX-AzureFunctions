using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Contract;

/// <summary>
/// Provides caching of credentials used to integrate with external services.
/// </summary>
public interface IIntegrationRepository
{
    /// <summary>
    /// Gets the integration credentials for a service.
    /// </summary>
    /// <param name="serviceName">The name of the service to get integration credentials for.</param>
    /// <returns>The integration credentials for the specified service, or null if not found.</returns>
    Task<IntegrationCredentials?> GetIntegrationCredentialsForServiceAsync(string serviceName);

    /// <summary>
    /// Sets the bearer and refresh tokens for a service.
    /// </summary>
    /// <param name="serviceName">The name of the service to set integration credentials for.</param>
    /// <param name="clientId">The Client ID used to authenticate with the external service.</param>
    /// <param name="bearerToken">The bearer token to set for the service.</param>
    /// <param name="refreshToken">The refresh token to set for the service.</param>
    Task SetBearerAndRefreshTokensAsync(string serviceName, string clientId, string bearerToken, string refreshToken);

}