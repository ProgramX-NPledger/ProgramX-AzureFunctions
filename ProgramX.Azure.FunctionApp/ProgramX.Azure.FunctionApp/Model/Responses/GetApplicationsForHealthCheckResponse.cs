using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetApplicationsForHealthCheckResponse
{
    [JsonPropertyName("timeStamp")]
    public DateTime TimeStamp {
        get;
        set;
    }
    
    [JsonPropertyName("healthCheckServices")] 
    public IList<ApplicationHealthCheckService> HealthCheckServices { get; set; } = [];

}