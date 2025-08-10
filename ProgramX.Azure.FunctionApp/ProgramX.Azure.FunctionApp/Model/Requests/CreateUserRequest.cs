using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class CreateUserRequest
{
    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; set; }
    
    [JsonPropertyName("userName")]
    public required string UserName { get; set; }
    
    [JsonPropertyName("password")]
    public required string Password { get; set; }

    [JsonPropertyName("addToRoles")]
    public required IEnumerable<Role> AddToRoles { get; set; }
    
}