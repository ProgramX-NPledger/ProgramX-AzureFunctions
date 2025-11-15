using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class HealthCheckItemResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("isHealthy")]
    public bool IsHealthy { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
}