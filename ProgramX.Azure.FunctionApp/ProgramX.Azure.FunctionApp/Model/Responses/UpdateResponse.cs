using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class UpdateResponse
{
    
    [JsonPropertyName("isOk")]
    public bool IsOk { get; set; }
    
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("httpEventType")]
    public HttpEventType HttpEventType { get; set; }

    [JsonPropertyName("bytesTransferred")]
    public long? BytesTransferred { get; set; } = null;

    [JsonPropertyName("totalBytesToTransfer")]
    public long? TotalBytesToTransfer { get; set; } = null;
}