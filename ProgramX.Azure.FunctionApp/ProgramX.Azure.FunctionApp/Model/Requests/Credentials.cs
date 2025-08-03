using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class Credentials
{
    [JsonPropertyName("userName")]
    public string UserName { get; set; }
    [JsonPropertyName("password")]
    public string Password { get; set; }
    
}