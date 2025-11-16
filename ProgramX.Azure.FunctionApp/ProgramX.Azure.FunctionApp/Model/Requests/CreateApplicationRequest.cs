namespace ProgramX.Azure.FunctionApp.Model.Requests;

/// <summary>
/// Represents a request to create an Application.
/// </summary>
public class CreateApplicationRequest
{
    /// <summary>
    /// Name of the Application.
    /// </summary>
    public required string name { get; set; }
    
    /// <summary>
    /// Description of the Application.
    /// </summary>
    public string? description { get; set; }
    
    /// <summary>
    /// Image URL of the Application.
    /// </summary>
    public string? imageUrl { get; set; }
    
    /// <summary>
    /// Target URL of the Application.
    /// </summary>
    public required string targetUrl { get; set; }
    
    /// <summary>
    /// Whether the Application is the default application to use on login.
    /// </summary>
    public bool isDefaultApplicationOnLogin { get; set; } = false;

    /// <summary>
    /// Order of the Application in the list of Applications.
    /// </summary>
    public int ordinal { get; set; }

    /// <summary>
    /// Roles to add the Application to.
    /// </summary>
    public IEnumerable<string> addToRoles { get; set; } = [];

}