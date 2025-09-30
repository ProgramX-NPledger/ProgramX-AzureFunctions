using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class PagedResponse<T>
{
    [JsonPropertyName("items")]
    public IEnumerable<T> Items { get; set; }
    
    [JsonPropertyName("nextPageUrl")]

    public string? NextPageUrl { get; set; }
    
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
    [JsonPropertyName("itemsPerPage")]
    public int? ItemsPerPage { get; set; }
    [JsonPropertyName("isLastPage")]
    public bool IsLastPage { get; set; }
    
}

