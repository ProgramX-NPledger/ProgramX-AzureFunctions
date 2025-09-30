using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Helpers;

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
    
    public double RequestCharge { get; set; }

    public int EstimatedTotalPageCount { get; set;  }

    public PagedResponse(PagedCosmosDBResult<T> pagedCosmosDBResult, string? nextPageUrl)
    {
        NextPageUrl = nextPageUrl;
        ContinuationToken = pagedCosmosDBResult.ContinuationToken;
        Items = pagedCosmosDBResult.Items;
        IsLastPage = !pagedCosmosDBResult.IsMorePages();
        ItemsPerPage = pagedCosmosDBResult.MaximumItemsRequested;
        RequestCharge = pagedCosmosDBResult.RequestCharge;
        EstimatedTotalPageCount = pagedCosmosDBResult.EstimatedTotalPageCount;
    }
}

