namespace ProgramX.Azure.FunctionApp.Model;

public class ApplicationMetaData
{
    /// <summary>
    /// Name of the Application. This must be the same as the identifier in the Cosmos DB record.
    /// </summary>
    public required string name { get; set; }

    /// <summary>
    /// Friendly name of the Application.
    /// </summary>
    public required string FriendlyName { get; set; }
    
    /// <summary>
    /// Description of the Application.
    /// </summary>
    public string? description { get; set; }
    
    /// <summary>
    /// URL of the image to use for the Application.
    /// </summary>
    public string? imageUrl { get; set; }
    
    /// <summary>
    /// URL to start the Application.
    /// </summary>
    public required string targetUrl { get; set; }
    
    /// <summary>
    /// Roles that the Application requires for full operation.
    /// </summary>
    public required string[] requiresRoleNames { get; set; }
}