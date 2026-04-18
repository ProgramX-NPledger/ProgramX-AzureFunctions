using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class CreateScoutingScoreItemRequest
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("osmScoutId")]
    public int OsmScoutId { get; set; }
    
    [JsonPropertyName("osmPatrolId")]
    public int OsmPatrolId { get; set; }
    
    [JsonPropertyName("patrolName")]
    public string PatrolName { get; set; }
    
    [JsonPropertyName("scoreName")]
    public string ScoreName { get; set; }
    
    [JsonPropertyName("score")]
    public int Score { get; set; }
    
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
    
}