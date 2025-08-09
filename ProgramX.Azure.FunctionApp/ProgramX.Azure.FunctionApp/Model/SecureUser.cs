namespace ProgramX.Azure.FunctionApp.Model;

public class SecureUser
{
    public required string id { get; set; }
    public required string userName { get; set; }
    public required string emailAddress { get; set; }

    public  IEnumerable<Role> roles { get; set; }
    
    
    // public Application[] applications { get; set; } = [];
    //
    // public string[] roles { get; set; } = [];


}