namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class CreateRoleRequest
{
    public string name { get; set; }
    public string description { get; set; }
    public IEnumerable<string> addToUsers { get; set; }
    public IEnumerable<string> addToApplications { get; set; }
    
}