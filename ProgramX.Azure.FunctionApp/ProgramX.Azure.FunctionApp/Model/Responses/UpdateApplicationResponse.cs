using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

/// <summary>
/// Represents the response to an update application request.
/// </summary>
public class UpdateApplicationResponse : UpdateResponse
{
    /// <summary>
    /// The name of the application.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}