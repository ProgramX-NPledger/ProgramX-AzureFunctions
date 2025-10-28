using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class Role
{

    [JsonPropertyName("name")]
    public string name { get; set; }
    public string description { get; set; }
    public IEnumerable<Application> applications { get; set; }
    public string type { get; } = "role";

    public int schemaVersionNumber { get; set;  } = 1;

    public DateTime? createdAt { get; set; }
    public DateTime? updatedAt { get; set; }
    
    
}