using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class Role
{

    public string name { get; set; }
    public string description { get; set; }
    public IEnumerable<Application> applications { get; set; }
    public string type { get; } = "role";

    public int versionNumber { get; } = 1;
    
}