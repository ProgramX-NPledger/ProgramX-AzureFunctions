using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class CreateScoreRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("score")]
    public int Score { get; set; }
    
    
}