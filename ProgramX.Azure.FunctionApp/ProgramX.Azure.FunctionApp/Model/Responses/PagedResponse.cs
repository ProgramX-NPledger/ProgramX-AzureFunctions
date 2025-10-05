using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Helpers;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class PagedResponse<T>
{
    [JsonPropertyName("items")]
    public IEnumerable<T> Items { get; set; }
    
    [JsonPropertyName("nextPageUrl")]

    public string? NextPageUrl { get; set; }

    [JsonPropertyName("pagesWithUrls")]
    public IEnumerable<UrlAccessiblePage> PagesWithUrls { get; set; }

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
    [JsonPropertyName("itemsPerPage")]
    public int? ItemsPerPage { get; set; }
    [JsonPropertyName("isLastPage")]
    public bool IsLastPage { get; set; }
    
    [JsonPropertyName("requestCharge")]
    public double RequestCharge { get; set; }

    [JsonPropertyName("estimatedTotalPageCount")]
    public int EstimatedTotalPageCount { get; set;  }
    
    [JsonPropertyName("timeDelta")]
    public TimeSpan? TimeDelta { get; set; }

    

    public PagedResponse(PagedCosmosDBResult<T> pagedCosmosDBResult, string? nextPageUrl, IEnumerable<UrlAccessiblePage> pagesWithUrls)
    {
        NextPageUrl = nextPageUrl;
        PagesWithUrls = pagesWithUrls;
        ContinuationToken = pagedCosmosDBResult.ContinuationToken;
        Items = pagedCosmosDBResult.Items;
        IsLastPage = !pagedCosmosDBResult.IsMorePages();
        ItemsPerPage = pagedCosmosDBResult.MaximumItemsRequested;
        RequestCharge = pagedCosmosDBResult.RequestCharge;
        TimeDelta = pagedCosmosDBResult.TimeDelta;
        EstimatedTotalPageCount = pagedCosmosDBResult.TotalItems;
    }
}

