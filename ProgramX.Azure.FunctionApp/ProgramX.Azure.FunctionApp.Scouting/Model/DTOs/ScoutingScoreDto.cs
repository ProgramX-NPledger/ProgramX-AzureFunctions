using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Scouting.Model.DTOs;

public class ScoutingScoreDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("score")]
    public int Score { get; set; }
    
    [JsonPropertyName("isDynamicallyCalculated")]
    public bool IsDynamicallyCalculated { get; set; }
    
    [JsonPropertyName("ordinal")]
    public int Ordinal { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    
}