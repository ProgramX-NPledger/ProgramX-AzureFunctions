using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class CreateUserRequest
{
    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; set; }
    
    [JsonPropertyName("userName")]
    public required string UserName { get; set; }
    
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }
    
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }
    
    [JsonPropertyName("addToRoles")]
    public required IEnumerable<string> AddToRoles { get; set; }

    [JsonPropertyName("passwordConfirmationLinkExpiryDate")]
    public DateTime? PasswordConfirmationLinkExpiryDate { get; set; }

}