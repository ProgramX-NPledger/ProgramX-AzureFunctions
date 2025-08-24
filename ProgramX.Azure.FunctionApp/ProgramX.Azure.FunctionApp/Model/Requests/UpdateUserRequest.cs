using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class UpdateUserRequest
{
    [JsonPropertyName("emailAddress")]
    public string? emailAddress { get; set; }
    
    [JsonPropertyName("userName")]
    public required string userName { get; set; }

    public string? firstName { get; set; }

    public string? lastName { get; set; }

    public  bool updateProfileScope { get; set; } = false;

    public bool updatePasswordScope { get; set; } = false;

    public bool updateProfilePictureScope { get; set; } = false;
    
    public bool updateRolesScope { get; set; } = false;

    public string? newPassword { get; set; }

    public string? confirmPassword { get; set; }

    public byte[] photo { get; set; } = [];

    [JsonPropertyName("roles")] public IEnumerable<Role> roles { get; set; } = [];

}