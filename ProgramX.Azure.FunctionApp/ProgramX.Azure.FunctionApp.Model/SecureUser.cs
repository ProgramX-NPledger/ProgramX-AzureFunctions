using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class SecureUser
{
    public required string id { get; set; }
    public required string userName { get; set; }
    public required string emailAddress { get; set; }
    public  IEnumerable<Role> roles { get; set; } 

    public string firstName { get; set; }
    public string lastName { get; set; }
    public string? profilePhotographSmall { get; set; }
    public string? profilePhotographOriginal { get; set; }
    public string theme { get; set; }

    public int schemaVersionNumber { get; set; } = 1;
    
    
    public string type { get; } = "user";
    
    
    public DateTime? createdAt { get; set; }
    public DateTime? updatedAt { get; set; }
    public DateTime? lastLoginAt { get; set; }
    public DateTime? lastPasswordChangeAt { get; set; }
    public DateTime? passwordLinkExpiresAt { get; set; }
    public string? passwordConfirmationNonce { get; set; }   

    


}