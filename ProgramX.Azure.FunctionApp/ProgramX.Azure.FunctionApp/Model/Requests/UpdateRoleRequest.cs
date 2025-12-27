using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

/// <summary>
/// Represents a request to update a Role.
/// </summary>
public class UpdateRoleRequest
{
    /// <summary>
    /// Name of the Role.
    /// </summary>
    public string? name { get; set; }
    
    /// <summary>
    /// Description of the Role.
    /// </summary>
    public string? description { get; set; }

    /// <summary>
    /// If set, contains usernames in the role. If <c>null</c> no changesto users are made.
    /// </summary>
    [JsonPropertyName("usersInRole")]
    public string[]? usersInRole { get; set; }

    [JsonPropertyName("applications")] public IEnumerable<string> applications { get; set; } = [];


    
}