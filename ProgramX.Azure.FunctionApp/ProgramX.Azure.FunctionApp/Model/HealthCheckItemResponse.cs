using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

/// <summary>
/// Represents a response to a health check request.
/// </summary>
public class HealthCheckItemResponse
{
    /// <summary>
    /// The internal name of the health check item.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    /// <summary>
    /// Whether the health check item is healthy.
    /// </summary>
    [JsonPropertyName("isHealthy")]
    public bool IsHealthy { get; set; }
    
    /// <summary>
    /// A message describing the health check item.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// The timestamp indicating when the health check item was executed.
    /// </summary>
    [JsonPropertyName("timeStamp")]
    public DateTime TimeStamp { get; set; }
    
    /// <summary>
    /// The sub items of the health check item.
    /// </summary>
    public IEnumerable<HealthCheckItemResult> SubItems { get; set; } = new List<HealthCheckItemResult>();
}