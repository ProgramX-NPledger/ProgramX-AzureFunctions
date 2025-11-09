using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class UpdateRoleRequest
{
    public string? name { get; set; }
    
    public string? decription { get; set; }
    

    [JsonPropertyName("applications")] public IEnumerable<string> applications { get; set; } = [];


    
}