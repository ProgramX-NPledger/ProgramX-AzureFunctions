using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class Application
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }
    [JsonPropertyName("targetUrl")]
    public required string TargetUrl { get; set; }
    public string type { get; } = "application";

    public int versionNumber { get; } = 1;
    
}