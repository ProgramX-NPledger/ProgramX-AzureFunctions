using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

/// <summary>
/// Represents an item in the health check response.
/// </summary>
public class HealthCheckItem
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
    /// If the health check item is implicit, this property will contain the response.
    /// </summary>
    [JsonPropertyName("immediateHealthCheckResponse")]
    public HealthCheckItemResponse? ImmediateHealthCheckResponse { get; set; }
}