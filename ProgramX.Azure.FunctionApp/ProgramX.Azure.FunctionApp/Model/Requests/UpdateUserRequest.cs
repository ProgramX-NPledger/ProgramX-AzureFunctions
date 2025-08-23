using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class UpdateUserRequest
{
    [JsonPropertyName("emailAddress")]
    public required string emailAddress { get; set; }
    
    [JsonPropertyName("userName")]
    public required string userName { get; set; }

    public required string firstName { get; set; }

    public required string lastName { get; set; }

    public required bool updateProfileScope { get; set; }

    public required bool updatePasswordScope { get; set; }
    
    public required bool updateRolesScope { get; set; }

    public required string newPassword { get; set; }

    public required string confirmPassword { get; set; }


    [JsonPropertyName("roles")]
    public required IEnumerable<Role> roles { get; set; }
    
}