using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

/// <summary>
/// Represents an item in the health check response.
/// </summary>
public class HealthCheckService
{
    /// <summary>
    /// Friendly name of the health check item.
    /// </summary>
    [JsonPropertyName("friendlyName")]
    public string? FriendlyName { get; set; }
    
    /// <summary>
    /// Image URL of the health check item.
    /// </summary>
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// The internal name of the health check item.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The URL to use to get the detail for the health check service.
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}