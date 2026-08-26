using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.AzureStorage;

public class BlobIndexEntry
{
    [JsonPropertyName("originalFileName")]
    public string OriginalFileName { get; set; }
    
    [JsonPropertyName("readRequiresRoles")]
    public string[] ReadRequiresRoles { get; set; }

    [JsonPropertyName("storedFileName")]
    public string StoredFileName { get; set; }

    [JsonPropertyName("containerName")]
    public string ContainerName { get; set; }

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }
    
}