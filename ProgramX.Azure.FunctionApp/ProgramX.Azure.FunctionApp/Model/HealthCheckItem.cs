using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class HealthCheckItem
{
    [JsonPropertyName("friendlyName")]
    public string FriendlyName { get; set; }
    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("immediateHealthCheckResponse")]
    public HealthCheckItemResponse? ImmediateHealthCheckResponse { get; set; }
}