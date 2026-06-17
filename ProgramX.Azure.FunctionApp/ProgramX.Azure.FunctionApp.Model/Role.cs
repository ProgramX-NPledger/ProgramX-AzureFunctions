using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

/// <summary>
/// Represents a role of one or more <see cref="UserPassword"/>s.
/// </summary>
public class Role
{
    /// <summary>
    /// Unique identifier of the Role.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Name of the Role.
    /// </summary>
    public required string RoleName { get; set; }
    
    /// <summary>
    /// Description of the Role.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Type of model.
    /// </summary>
    public string Type { get; } = "role";

    /// <summary>
    /// The version number of the schema used to serialize this instance.
    /// </summary>
    public int SchemaVersionNumber { get; set;  } = 1;

    /// <summary>
    /// Time stamp of the item's creation.
    /// </summary>
    public DateTime? CreatedAt { get; set; }
    
    /// <summary>
    /// Time stamp of the last update.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    
}