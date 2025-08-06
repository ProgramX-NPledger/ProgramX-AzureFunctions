namespace ProgramX.Azure.FunctionApp.Model;

public class User
{
    public required string id { get; set; }
    public required string userName { get; set; }
    public required string emailAddress { get; set; }

    public required byte[] passwordHash { get; set; }

    public required byte[] passwordSalt { get; set; }
    
}