using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class UserPassword 
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
    /// Gets or sets the password hash.
    /// </summary>
    public required byte[] passwordHash { get; set; }
    
    /// <summary>
    /// Gets or sets the password salt.
    /// </summary>
    public required byte[] passwordSalt { get; set; }
    

    
}