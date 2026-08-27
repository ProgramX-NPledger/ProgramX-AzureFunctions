namespace ProgramX.Azure.FunctionApp.Scouting.Model;

/// <summary>
/// Represents an applied Score for a Scouting Member.
/// </summary>
public class ScoutingScoreItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// This property is required and is intended to uniquely identify a User instance within the system.
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// The Member ID within OSM. This avoids storage of PII outside of OSM.
    /// </summary>
    public int OsmMemberId { get; set; }
    
    /// <summary>
    /// Date score was applied,
    /// </summary>
    public DateOnly Date { get; set; }
    
    /// <summary>
    /// Notes attached to the score.
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Name of the Score.
    /// </summary>
    public string ScoreName { get; set; }
    
    /// <summary>
    /// Name of the Patrol to Member is a member of.
    /// </summary>
    public string? PatrolName  { get; set; }
    
    /// <summary>
    /// The score value.
    /// </summary>
    public int Score { get; set; }
    
    /// <summary>
    /// The version number of the schema used to serialize this instance.
    /// </summary>
    public int SchemaVersionNumber { get; set; }
    
    /// <summary>
    /// Time stamp of the item's creation.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Time stamp of the last update.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// The type of the model
    /// </summary>
    public string Type { get; } = "scouting-score-item";
}