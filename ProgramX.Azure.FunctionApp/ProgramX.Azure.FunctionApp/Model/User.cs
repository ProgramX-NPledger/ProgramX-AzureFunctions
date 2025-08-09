using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class User
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("UserName")]
    public required string UserName { get; set; }
    [JsonPropertyName("EmailAddress")]
    public required string EmailAddress { get; set; }
    [JsonPropertyName("PasswordHash")]
    public required byte[] PasswordHash { get; set; }
    [JsonPropertyName("PasswordSalt")]
    public required byte[] PasswordSalt { get; set; }
}