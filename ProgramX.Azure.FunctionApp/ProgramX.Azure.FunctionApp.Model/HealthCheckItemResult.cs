using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class HealthCheckItemResult
{
    /// <summary>
    /// Whether the check is healthy. If <c>null</c>, the check has not been run yet.
    /// </summary>
    [JsonPropertyName("isHealthy")]
    public bool? IsHealthy { get; set; }
    
    /// <summary>
    /// Friendly text describing the check.
    /// </summary>
    [JsonPropertyName("friendlyName")]
    public required string FriendlyName { get; set; }
    
    /// <summary>
    /// Additional information about the check.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Name of the check
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}