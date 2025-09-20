using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class CreateUserRequest
{
    public required string emailAddress { get; set; }
    
    public required string userName { get; set; }
    
    public required string firstName { get; set; }
    
    public required string lastName { get; set; }
    
    public string? password { get; set; }

    public required IEnumerable<string> addToRoles { get; set; }

    public DateTime? passwordConfirmationLinkExpiryDate { get; set; }

    
}