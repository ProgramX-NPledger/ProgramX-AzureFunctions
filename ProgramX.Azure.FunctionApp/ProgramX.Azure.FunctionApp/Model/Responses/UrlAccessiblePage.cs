using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class UrlAccessiblePage
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
    [JsonPropertyName("isCurrentPage")]
    public bool IsCurrentPage { get; set; }
    
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }
}