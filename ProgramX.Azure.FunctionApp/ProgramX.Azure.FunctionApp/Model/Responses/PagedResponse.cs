using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Helpers;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class PagedResponse<T>
{
    [JsonPropertyName("items")]
    public IEnumerable<T> Items { get; set; }
    

    [JsonPropertyName("pagesWithUrls")]
    public IEnumerable<UrlAccessiblePage> PagesWithUrls { get; set; }

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
    [JsonPropertyName("itemsPerPage")]
    public int ItemsPerPage { get; set; }
    
    [JsonPropertyName("isLastPage")]
    public bool IsLastPage { get; set; }
    
    [JsonPropertyName("requestCharge")]
    public double RequestCharge { get; set; }
    
    [JsonPropertyName("timeDeltaMs")]
    public double TimeDeltaMs { get; set; }

    [JsonPropertyName("totalItems")]
    public long TotalItems { get; set; }
        

    public PagedResponse(PagedCosmosDbResult<T> pagedCosmosDbResult, IEnumerable<UrlAccessiblePage> pagesWithUrls)
    {
        PagesWithUrls = pagesWithUrls;
        ContinuationToken = pagedCosmosDbResult.ContinuationToken;
        Items = pagedCosmosDbResult.Items;
        if (!string.IsNullOrEmpty(pagedCosmosDbResult.ContinuationToken))
        {
            IsLastPage = !pagedCosmosDbResult.IsMorePages();    
        }
        
        ItemsPerPage = pagedCosmosDbResult.MaximumItemsRequested ?? DataConstants.ItemsPerPage;
        RequestCharge = pagedCosmosDbResult.RequestCharge;
        TimeDeltaMs = pagedCosmosDbResult.TimeDeltaMs;
        TotalItems = pagedCosmosDbResult.TotalItems;
    }
}

