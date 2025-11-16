using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

/// <summary>
/// Represents the response to an update role request.
/// </summary>
public class UpdateRoleResponse : UpdateResponse
{
    /// <summary>
    /// Name of the role.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}