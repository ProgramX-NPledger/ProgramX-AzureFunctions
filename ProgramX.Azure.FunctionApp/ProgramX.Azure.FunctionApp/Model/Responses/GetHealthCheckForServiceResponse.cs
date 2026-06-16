using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

/// <summary>
/// Represents the response to a health check request.
/// </summary>
public class GetHealthCheckForServiceResponse
{
    /// <summary>
    /// Name of the service. Use this when requesting for a Health Check or identifying the service.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    /// <summary>
    /// The time stamp of the health check item.
    /// </summary>
    [JsonPropertyName("timeStamp")]
    public DateTime TimeStamp { get; set; }
    
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
    /// The sub items of the health check item.
    /// </summary>
    [JsonPropertyName("subItems")]
    public IEnumerable<ServiceHealthCheckItemResult> SubItems { get; set; } = new List<ServiceHealthCheckItemResult>();

}