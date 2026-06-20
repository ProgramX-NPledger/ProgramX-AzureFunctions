using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses.Dtos;

/// <summary>
/// Represents a User able to log in to the application. This is a secure version of the User model and does not include sensitive information.
/// </summary>
public class UserDto
{
    /// <summary>
    /// Gets or sets the username of the user.
    /// This property is required and is intended to serve as a unique logical identifier for a user's login within the system.
    /// </summary>
    [JsonPropertyName("userName")]
    public required string UserName { get; set; }

    /// <summary>
    /// Gets or sets the email address associated with the user.
    /// This property is required and is used to identify and communicate with the user.
    /// </summary>
    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; set; }

    /// <summary>
    /// Gets or sets the collection of roles associated with the user.
    /// This property represents the roles assigned to a user within the system,
    /// which define their permissions and access levels.
    /// </summary>
    [JsonPropertyName("roles")]
    public required IEnumerable<string> Roles { get; set; } 

    /// <summary>
    /// Gets or sets the first name of the user.
    /// This property represents the given name or personal name
    /// associated with a SecureUser instance.
    /// </summary>
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }
    
    /// <summary>
    /// The last name of the User.
    /// </summary>
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }
    
    /// <summary>
    /// Filename of the profile photograph for the User with smaller dimensions.
    /// </summary>
    [JsonPropertyName("profilePhotographSmall")]
    public string? ProfilePhotographSmall { get; set; }
    
    /// <summary>
    /// Filename of the profile photograph for the User with original dimensions.
    /// </summary>
    [JsonPropertyName("profilePhotographOriginal")]
    public string? ProfilePhotographOriginal { get; set; }
    
    /// <summary>
    /// The theme to use for the User.
    /// </summary>
    [JsonPropertyName("theme")]
    public string? Theme { get; set; }
    
    /// <summary>
    /// Time stamp of the item's creation.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    
    /// <summary>
    /// Time stamp of the last update.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Time stamp of the last login.
    /// </summary>
    [JsonPropertyName("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// Time stamp of the last password change.
    /// </summary>
    [JsonPropertyName("lastPasswordChangeAt")]
    public DateTime? LastPasswordChangeAt { get; set; }
    
    /// <summary>
    /// When the password reset link expires, if any.
    /// </summary>
    [JsonPropertyName("passwordLinkExpiresAt")]
    public DateTime? PasswordLinkExpiresAt { get; set; }
    


}