using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

/// <summary>
/// Represents a request to update a user.
/// </summary>
public class UpdateUserSettingsRequest
{
    /// <summary>
    /// The User's preferred theme. 
    /// </summary>
    [JsonPropertyName("theme")]
    public string? Theme { get; set; }
    

}