using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class User : SecureUser
{
    
    
    public required byte[] passwordHash { get; set; }
    
    public required byte[] passwordSalt { get; set; }
    

    
}