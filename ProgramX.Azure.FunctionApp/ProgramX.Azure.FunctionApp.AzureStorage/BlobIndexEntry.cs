using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.AzureStorage;

public class BlobIndexEntry
{
    [JsonPropertyName("originalFileName")]
    public string OriginalFileName { get; set; }
    
    [JsonPropertyName("readRequiresRoles")]
    public string[] ReadRequiresRoles { get; set; }
    
}