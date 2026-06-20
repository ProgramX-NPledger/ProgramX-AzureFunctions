using System.Collections.Specialized;
using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class HealthCheckResult
{
    [JsonPropertyName("isHealthy")]
    public bool IsHealthy { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("healthCheckName")]
    public required string HealthCheckName { get; set; }

    [JsonPropertyName("friendlyName")]
    public required string FriendlyName { get; set; }
    
}