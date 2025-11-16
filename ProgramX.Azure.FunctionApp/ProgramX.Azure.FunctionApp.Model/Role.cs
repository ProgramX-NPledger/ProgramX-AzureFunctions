using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

/// <summary>
/// Represents a role of one or more <see cref="User"/>s.
/// </summary>
public class Role
{

    /// <summary>
    /// Name of the Role.
    /// </summary>
    [JsonPropertyName("name")]
    public required string name { get; set; }
    
    /// <summary>
    /// Description of the Role.
    /// </summary>
    public string? description { get; set; }
    
    /// <summary>
    /// <see cref="Application"/>s that use this Role.
    /// </summary>
    public IEnumerable<Application> applications { get; set; } = [];
    
    /// <summary>
    /// Type of model.
    /// </summary>
    public string type { get; } = "role";

    /// <summary>
    /// The version number of the schema used to serialize this instance.
    /// </summary>
    public int schemaVersionNumber { get; set;  } = 1;

    /// <summary>
    /// Time stamp of the item's creation.
    /// </summary>
    public DateTime? createdAt { get; set; }
    
    /// <summary>
    /// Time stamp of the last update.
    /// </summary>
    public DateTime? updatedAt { get; set; }
    
    
}