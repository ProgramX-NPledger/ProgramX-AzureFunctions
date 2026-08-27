namespace ProgramX.Azure.FunctionApp.Scouting.Model;

/// <summary>
/// Represents a Scouting Score that may be applied to a Scouting Member.
/// </summary>
public class ScoutingScore
{
    
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// This property is required and is intended to uniquely identify a User instance within the system.
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// Name of the score.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Value of the Score.
    /// </summary>
    public int Score { get; set; }
    
    /// <summary>
    /// Whether the Score is dynamically calculated and is read-only.
    /// </summary>
    public bool IsDynamicallyCalculated { get; set; }
    
    /// <summary>
    /// Ordinal of the Score in relation to other Scores.
    /// </summary>
    public int Ordinal { get; set; }

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
}