using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

/// <summary>
/// Represents the response to an update user request.
/// </summary>
public class UpdateUserResponse : UpdateResponse
{
    /// <summary>
    /// Username of the user.
    /// </summary>
    [JsonPropertyName("userName")]
    public required string Username { get; set; }
}