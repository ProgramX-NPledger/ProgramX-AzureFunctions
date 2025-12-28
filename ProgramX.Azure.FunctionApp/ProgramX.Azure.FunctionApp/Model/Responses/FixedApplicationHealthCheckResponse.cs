using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class FixedApplicationHealthCheckResponse
{
    [JsonPropertyName( "healthCheckName")]
    public string HealthCheckName { get; set; }
    
    [JsonPropertyName("messages")]
    public IEnumerable<string> Messages { get; set; }
}