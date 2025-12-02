using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetHealthCheckServiceResponse
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
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
    public IEnumerable<HealthCheckItemResult> SubItems { get; set; } = new List<HealthCheckItemResult>();

}