using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class LoginResponse
{
    [JsonPropertyName( "token")]
    public required string Token { get; set; }

    [JsonPropertyName("userName")]
    public required string UserName { get; set; }

    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; set; }

    [JsonPropertyName("memberOfRoles")]
    public IEnumerable<string> MemberOfRoles { get; set; }
    
    [JsonPropertyName("canUseApplications")]
    public IEnumerable<string> CanUseApplications { get; set; }
    
    [JsonPropertyName("profilePhotoBase64")]
    public string? ProfilePhotoBase64 { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }
    
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }
    
    [JsonPropertyName("initials")]
    public string? Initials { get; set; }
    
    [JsonPropertyName("profilePhotographSmall")]
    public string? ProfilePhotographSmall { get; set; }
    
}