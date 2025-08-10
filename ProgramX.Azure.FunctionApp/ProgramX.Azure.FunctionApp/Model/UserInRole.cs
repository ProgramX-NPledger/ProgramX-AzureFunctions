using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class UserInRole
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("userName")]
    public required string UserName { get; set; }
    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; set; }


}