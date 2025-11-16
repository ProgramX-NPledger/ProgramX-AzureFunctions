namespace ProgramX.Azure.FunctionApp.Model.Responses;

/// <summary>
/// Represents a response to an update profile photo request.
/// </summary>
public class UpdateProfilePhotoResponse : UpdateResponse
{
    /// <summary>
    /// The URL of the profile photo.
    /// </summary>
    public string? photoUrl { get; set; }
    
    
}