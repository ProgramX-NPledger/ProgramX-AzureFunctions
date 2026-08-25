using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

/// <summary>
/// Represents a response to an update profile photo request.
/// </summary>
public class UpdateProfilePhotoResponse 
{
    /// <summary>
    /// The URL of the profile photo.
    /// </summary>
    [JsonPropertyName("photoUrl")]
    public string? PhotoUrl { get; set; }

    [JsonPropertyName("httpEventType")]
    public int HttpEventType { get; set; }

    [JsonPropertyName("totalBytesToTransfer")]
    public long TotalBytesToTransfer { get; set; }

    [JsonPropertyName("bytesTransferred")]
    public long BytesTransferred { get; set; }
    
}