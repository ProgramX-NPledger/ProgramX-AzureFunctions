using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class SecureUser
{
    [JsonPropertyName("id")]
    public required string id { get; set; }
    [JsonPropertyName("userName")]
    public required string userName { get; set; }
    [JsonPropertyName("emailAddress")]
    public required string emailAddress { get; set; }
    [JsonPropertyName("roles")]
    public  IEnumerable<Role> roles { get; set; }


}