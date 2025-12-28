using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetAllApplicationsHealthCheckResponse
{
    [JsonPropertyName("timeStamp")]
    public DateTime TimeStamp { get; set; }
    
    
    /// <summary>
    /// Whether the health check item is healthy.
    /// </summary>
    [JsonPropertyName("isHealthy")]
    public bool IsHealthy { get; set; }
    
    /// <summary>
    /// The sub items of the health check item.
    /// </summary>
    [JsonPropertyName("applicationHealthChecks")]
    public IEnumerable<HealthCheckResult> ApplicationHealthChecks { get; set; } = new List<HealthCheckResult>();

}