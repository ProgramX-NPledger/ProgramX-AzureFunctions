using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Scouting.Model.DTOs;

public class ScoutingScoreItemDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// This property is required and is intended to uniquely identify a User instance within the system.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    /// <summary>
    /// Member identifier within OSM. This avoids storage of PII outside of OSM.
    /// </summary>
    [JsonPropertyName("osmMemberId")]
    public int OsmMemberId { get; set; }

    /// <summary>
    /// Member name, from OSM
    /// </summary>
    [JsonPropertyName("memberName")]
    public string MemberName { get; set; }
    
    /// <summary>
    /// Date score was applied,
    /// </summary>
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }
    
    /// <summary>
    /// Notes attached to the score.
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Name of the Score.
    /// </summary>
    [JsonPropertyName("scoreName")]
    public string ScoreName { get; set; }
    
    /// <summary>
    /// Name of the Patrol to Member is a member of.
    /// </summary>
    [JsonPropertyName("patrolName")]
    public string? PatrolName  { get; set; }
    
    /// <summary>
    /// The score value.
    /// </summary>
    [JsonPropertyName("score")]
    public int Score { get; set; }
    
    /// <summary>
    /// Time stamp of the item's creation.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Time stamp of the last update.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}