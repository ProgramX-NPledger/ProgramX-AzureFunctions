using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

/// <summary>
/// Represents a User able to log in to the application. This is a secure version of the User model and does not include sensitive information.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// This property is required and is intended to uniquely identify a User instance within the system.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the username of the user.
    /// This property is required and is intended to serve as a unique logical identifier for a user's login within the system.
    /// </summary>
    public required string UserName { get; set; }

    /// <summary>
    /// Gets or sets the email address associated with the user.
    /// This property is required and is used to identify and communicate with the user.
    /// </summary>
    public required string EmailAddress { get; set; }

    /// <summary>
    /// Gets or sets the collection of roles associated with the user.
    /// This property represents the roles assigned to a user within the system,
    /// which define their permissions and access levels.
    /// </summary>
    public required IEnumerable<string> Roles { get; set; } 

    /// <summary>
    /// Gets or sets the first name of the user.
    /// This property represents the given name or personal name
    /// associated with a SecureUser instance.
    /// </summary>
    public string? FirstName { get; set; }
    
    /// <summary>
    /// The last name of the User.
    /// </summary>
    public string? LastName { get; set; }
    
    /// <summary>
    /// Filename of the profile photograph for the User with smaller dimensions.
    /// </summary>
    public string? ProfilePhotographSmall { get; set; }
    
    /// <summary>
    /// Filename of the profile photograph for the User with original dimensions.
    /// </summary>
    public string? ProfilePhotographOriginal { get; set; }
    
    /// <summary>
    /// The theme to use for the User.
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// The version number of the schema used to serialize this instance.
    /// </summary>
    public int SchemaVersionNumber { get; set; } = 1;
    
    /// <summary>
    /// The type of the model
    /// </summary>
    public string Type { get; } = "user";
    
    /// <summary>
    /// Time stamp of the item's creation.
    /// </summary>
    public DateTime? CreatedAt { get; set; }
    
    /// <summary>
    /// Time stamp of the last update.
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Time stamp of the last login.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// Time stamp of the last password change.
    /// </summary>
    [Obsolete("Use LastPasswordChangeAtUtc instead.")]
    public DateTime? LastPasswordChangeAt { get; set; }
    
    /// <summary>
    /// Time stamp of the last password change.
    /// </summary>
    public DateTime? LastPasswordChangeAtUtc { get; set; }
    
    /// <summary>
    /// When the password reset link expires, if any.
    /// </summary>
    public DateTime? PasswordLinkExpiresAt { get; set; }
    
    /// <summary>
    /// Used to verify that the password reset link is valid.
    /// </summary>
    public string? PasswordConfirmationNonce { get; set; }   

    


}