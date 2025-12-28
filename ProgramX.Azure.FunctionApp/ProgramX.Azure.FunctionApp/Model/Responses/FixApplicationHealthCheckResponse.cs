using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class FixApplicationHealthCheckResponse
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("timeStamp")]
    public DateTime TimeStamp { get; set; }

    public IEnumerable<FixedApplicationHealthCheckResponse> FixedApplicationHealthCheckResponses { get; set; }
    

}