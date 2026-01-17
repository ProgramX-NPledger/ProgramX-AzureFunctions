namespace ProgramX.Azure.FunctionApp.Model;

/// <summary>
/// Allows storage of tokens between requests to provide limited caching.
/// </summary>
public class IntegrationCredentials
{
    /// <summary>
    /// The name of the service to which the credentials are associated.
    /// </summary>
    public string serviceName { get; set; }
    
    /// <summary>
    /// The Client ID presented to the service.
    /// </summary>
    public string clientId { get; set; }
    
    /// <summary>
    /// The Bearer Token presented to the service.
    /// </summary>
    public string bearerToken { get; set; }
    
    /// <summary>
    /// The Refresh Token required to obtain a new Bearer Token.
    /// </summary>
    public string refreshToken { get; set; }
    
    /// <summary>
    /// The time at which the credentials were last updated.
    /// </summary>
    public DateTime lastUpdatedAt { get; set; }
}