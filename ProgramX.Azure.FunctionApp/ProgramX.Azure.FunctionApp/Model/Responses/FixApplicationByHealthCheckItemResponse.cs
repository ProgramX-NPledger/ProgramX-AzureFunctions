using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetHealthCheckServiceItemResponse
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("isSuccess")]
    public required bool IsSuccess { get; set; }
    
    [JsonPropertyName("messages")]
    public required IEnumerable<string> Messages { get; set; } = [];

}