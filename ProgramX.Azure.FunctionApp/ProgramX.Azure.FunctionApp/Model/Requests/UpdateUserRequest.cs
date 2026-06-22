using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

/// <summary>
/// Represents a request to update a user.
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// The new email address of the user. To update this, the user must have the "updateProfileScope" property set.
    /// </summary>
    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; set; }
    
    /// <summary>
    /// The new first and last name of the user. To update these, the user must have the "updateProfileScope" property set.
    /// </summary>
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    /// <summary>
    /// The new last name of the user. To update this, the user must have the "updateProfileScope" property set.
    /// </summary>
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }
    
    /// <summary>
    /// The new roles of the user. To set new roles, the user must have the "updateRolesScope" property set.
    /// </summary>
    [JsonPropertyName("roles")] 
    public IEnumerable<string> Roles { get; set; } = [];

}