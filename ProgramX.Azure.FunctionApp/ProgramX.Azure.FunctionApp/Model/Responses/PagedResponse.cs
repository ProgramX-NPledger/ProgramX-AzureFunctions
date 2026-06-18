using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model.Constants;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class PagedResponse<TPagedType, TDto>
{
    [JsonPropertyName("items")]
    public IEnumerable<TDto> Items { get; set; }
    

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
        

    public PagedResponse(IPagedResult<TPagedType> pagedResult, IEnumerable<UrlAccessiblePage> pagesWithUrls, Func<TPagedType, TDto> dtoConverter)
    {
        PagesWithUrls = pagesWithUrls;
        ContinuationToken = pagedResult.ContinuationToken;
        Items = pagedResult.Items.Select(dtoConverter); 
        ItemsPerPage = pagedResult.ItemsPerPage;
        if (pagedResult is IChargeableResult chargeableResult)
        {
            RequestCharge = chargeableResult.RequestCharge;    
        }
        TimeDeltaMs = pagedResult.TimeDeltaMs;
        TotalItems = pagedResult.TotalCount;
    }
}

