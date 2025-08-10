using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class User
{
    [JsonPropertyName("id")]
    public required string id { get; set; }
    
    [JsonPropertyName("userName")]
    public required string userName { get; set; }
    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; set; }
    [JsonPropertyName("passwordHash")]
    public required byte[] PasswordHash { get; set; }
    [JsonPropertyName("passwordSalt")]
    public required byte[] PasswordSalt { get; set; }
    [JsonPropertyName("roles")]
    public  IEnumerable<Role> Roles { get; set; }
}