using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

/// <summary>
/// Represents a User able to log in to the application. This is a secure version of the User model and does not include sensitive information.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// This property is required and is intended to uniquely identify a SecureUser instance within the system.
    /// </summary>
    public required string id { get; set; }

    /// <summary>
    /// Gets or sets the username of the user.
    /// This property is required and is intended to serve as a unique logical identifier for a user's login within the system.
    /// </summary>
    public required string userName { get; set; }

    /// <summary>
    /// Gets or sets the email address associated with the user.
    /// This property is required and is used to identify and communicate with the user.
    /// </summary>
    public required string emailAddress { get; set; }

    /// <summary>
    /// Gets or sets the collection of roles associated with the user.
    /// This property represents the roles assigned to a user within the system,
    /// which define their permissions and access levels.
    /// </summary>
    public required IEnumerable<Role> roles { get; set; } 

    /// <summary>
    /// Gets or sets the first name of the user.
    /// This property represents the given name or personal name
    /// associated with a SecureUser instance.
    /// </summary>
    public string? firstName { get; set; }
    
    /// <summary>
    /// The last name of the User.
    /// </summary>
    public string? lastName { get; set; }
    
    /// <summary>
    /// Filename of the profile photograph for the User with smaller dimensions.
    /// </summary>
    public string? profilePhotographSmall { get; set; }
    
    /// <summary>
    /// Filename of the profile photograph for the User with original dimensions.
    /// </summary>
    public string? profilePhotographOriginal { get; set; }
    
    /// <summary>
    /// The theme to use for the User.
    /// </summary>
    public string? theme { get; set; }

    /// <summary>
    /// The version number of the schema used to serialize this instance.
    /// </summary>
    public int schemaVersionNumber { get; set; } = 1;
    
    /// <summary>
    /// The type of the model
    /// </summary>
    public string type { get; } = "user";
    
    /// <summary>
    /// Time stamp of the item's creation.
    /// </summary>
    public DateTime? createdAt { get; set; }
    
    /// <summary>
    /// Time stamp of the last update.
    public DateTime? updatedAt { get; set; }
    
    /// <summary>
    /// Time stamp of the last login.
    /// </summary>
    public DateTime? lastLoginAt { get; set; }
    
    /// <summary>
    /// Time stamp of the last password change.
    /// </summary>
    public DateTime? lastPasswordChangeAt { get; set; }
    
    /// <summary>
    /// When the password reset link expires, if any.
    /// </summary>
    public DateTime? passwordLinkExpiresAt { get; set; }
    
    /// <summary>
    /// Used to verify that the password reset link is valid.
    /// </summary>
    public string? passwordConfirmationNonce { get; set; }   

    


}