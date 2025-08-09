using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class Role
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }
    [JsonPropertyName("applications")]
    public IEnumerable<Application> Applications { get; set; }
    
}