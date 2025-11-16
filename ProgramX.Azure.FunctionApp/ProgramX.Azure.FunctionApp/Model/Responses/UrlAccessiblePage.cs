using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

/// <summary>
/// Represents a page that is accessible via a URL.
/// </summary>
public class UrlAccessiblePage
{
    /// <summary>
    /// URL of the page.
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; set; }
    
    /// <summary>
    /// Whether the represented page is the current page.
    /// </summary>
    [JsonPropertyName("isCurrentPage")]
    public required bool IsCurrentPage { get; set; }
    
    /// <summary>
    /// The number of the page.
    /// </summary>
    [JsonPropertyName("pageNumber")]
    public required int PageNumber { get; set; }
}