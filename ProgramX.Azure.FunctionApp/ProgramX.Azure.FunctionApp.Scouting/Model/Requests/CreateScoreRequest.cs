using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Scouting.Model.Requests;

public class CreateScoreRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("score")]
    public int Score { get; set; }
    
    
}