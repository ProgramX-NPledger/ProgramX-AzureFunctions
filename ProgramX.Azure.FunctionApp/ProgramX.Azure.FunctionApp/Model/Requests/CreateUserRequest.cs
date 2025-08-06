namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class CreateUserRequest
{
    public string emailAddress { get; set; }
    public string userName { get; set; }
    public string password { get; set; }
    
}