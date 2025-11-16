using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

/// <summary>
/// Credentials for authentication.
/// </summary>
public class Credentials
{
    /// <summary>
    /// Username of user requiring authentication.
    /// </summary>
    [JsonPropertyName("userName")]
    public required string UserName { get; set; }
    
    /// <summary>
    /// Password of user requiring authentication.
    /// </summary>
    [JsonPropertyName("password")]
    public required string Password { get; set; }
    
}