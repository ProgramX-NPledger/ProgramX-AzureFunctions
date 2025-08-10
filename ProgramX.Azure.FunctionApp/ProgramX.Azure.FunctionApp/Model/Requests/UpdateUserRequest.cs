using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class UpdateUserRequest
{
    [JsonPropertyName("emailAddress")]
    public required string emailAddress { get; set; }
    
    [JsonPropertyName("userName")]
    public required string userName { get; set; }

    [JsonPropertyName("roles")]
    public required IEnumerable<Role> roles { get; set; }
    
}