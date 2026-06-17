using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

/// <summary>
/// Represents a request to create a Role.
/// </summary>
public class CreateRoleRequest
{
    /// <summary>
    /// Name of the Role.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    /// <summary>
    /// Description of the Role.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The Users to add to the Role.
    /// </summary>
    [JsonPropertyName("addToUsers")]
    public IEnumerable<string> AddToUsers { get; set; } = [];


}