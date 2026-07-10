using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

/// <summary>
/// Represents a request to update a Role.
/// </summary>
public class UpdateRoleRequest
{
    /// <summary>
    /// Description of the Role.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// If set, contains usernames in the role. If <c>null</c> no changes to users are made.
    /// </summary>
    [JsonPropertyName("usersInRole")]
    public string[]? UsersInRole { get; set; }
    
}