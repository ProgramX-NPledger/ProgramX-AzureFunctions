using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.DTOs;

public class ScoutingScoreItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("osmMemberId")]
    public int OsmMemberId { get; set; }
    
    [JsonPropertyName("date")] 
    public DateOnly Date { get; set; }
    
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
    
    [JsonPropertyName("scoreName")]
    public string ScoreName { get; set; }
    
    [JsonPropertyName("patrolName")]
    public string? PatrolName  { get; set; }
    
    [JsonPropertyName("score")]
    public int Score { get; set; }
    
    [JsonPropertyName("schemaVersionNumber")]
    public int SchemaVersionNumber { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}