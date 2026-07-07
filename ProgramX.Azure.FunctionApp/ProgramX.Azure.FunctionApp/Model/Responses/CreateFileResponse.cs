using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;


public class CreateFileResponse
{
    [JsonPropertyName("fileNames")]
    public IEnumerable<string> FileNames { get; set; }
    
    
}