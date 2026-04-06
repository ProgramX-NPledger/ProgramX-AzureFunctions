using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class ApplicationHealthCheckService
{
    [JsonPropertyName( "name")]
    public string Name { get; set; }
    
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("friendlyName")]
    public string FriendlyName { get; set; }
    
}