using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetApplicationsForHealthCheckResponse
{
    /// <summary>
    /// Timestamp of the health check.
    /// </summary>
    [JsonPropertyName("timeStamp")]
    public DateTime TimeStamp {
        get;
        set;
    }
    
    /// <summary>
    /// List of Applications that are eligible for health checks.
    /// </summary>
    [JsonPropertyName("healthCheckServices")] 
    public IList<ApplicationHealthCheckService> HealthCheckServices { get; set; } = [];

    /// <summary>
    /// Whether the user making the request is elevated and all Applications are returned.
    /// </summary>
    [JsonPropertyName("isElevated")]
    public bool IsElevated { get; set; } = false;
    
}