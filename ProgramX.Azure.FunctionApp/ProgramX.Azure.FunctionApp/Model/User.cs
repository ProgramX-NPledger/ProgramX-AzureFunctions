using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class User
{
    [JsonPropertyName("id")]
    public required string id { get; set; }
    
    [JsonPropertyName("userName")]
    public required string userName { get; set; }
    [JsonPropertyName("emailAddress")]
    public required string emailAddress { get; set; }
    [JsonPropertyName("passwordHash")]
    public required byte[] passwordHash { get; set; }
    [JsonPropertyName("passwordSalt")]
    public required byte[] passwordSalt { get; set; }
    [JsonPropertyName("roles")]
    public  IEnumerable<Role> roles { get; set; }

    public string type { get; } = "user";

    public int versionNumber { get; } = 1;

    public string firstName { get; set; }
    public string lastName { get; set; }

    public string profilePhotographSmall { get; set; }
    
    
}