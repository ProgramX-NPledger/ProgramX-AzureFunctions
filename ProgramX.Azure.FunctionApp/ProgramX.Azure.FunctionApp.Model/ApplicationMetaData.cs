using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class ApplicationMetaData
{
    /// <summary>
    /// Name of the Application. This must be the same as the identifier in the Cosmos DB record.
    /// </summary>
    [JsonPropertyName( "name" )]
    public required string Name { get; set; }

    /// <summary>
    /// Friendly name of the Application.
    /// </summary>
    [JsonPropertyName( "friendlyName")]
    public required string FriendlyName { get; set; }
    
    /// <summary>
    /// Description of the Application.
    /// </summary>
    [JsonPropertyName( "description" )]
    public string? Description { get; set; }
    
    /// <summary>
    /// URL of the image to use for the Application.
    /// </summary>
    [JsonPropertyName( "imageUrl" )]
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// URL to start the Application.
    /// </summary>
    [JsonPropertyName( "targetUrl" )]
    public required string TargetUrl { get; set; }
    
    /// <summary>
    /// Roles that the Application requires for full operation.
    /// </summary>
    [JsonPropertyName( "requiresRoleNames" )]
    public required string[] RequiresRoleNames { get; set; }
}