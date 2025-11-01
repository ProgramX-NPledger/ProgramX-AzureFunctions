using ProgramX.Azure.FunctionApp.Contract;

namespace ProgramX.Azure.FunctionApp.Cosmos;

/// <summary>
/// Represents a paged result from Cosmos DB.
/// </summary>
/// <typeparam name="T">Type of item in the result.</typeparam>
public class CosmosPagedResult<T> : CosmosResult<T>, IPagedResult<T>
{
    private CosmosPagedResult() 
    {
        
    }

    /// <summary>
    /// Creates a new instance of CosmosPagedResult.
    /// </summary>
    /// <param name="items">Items to return.</param>
    /// <param name="continuationToken">The continuation token to use for the next page.</param>
    /// <param name="itemsPerPage">The number of items per page.</param>
    /// <param name="totalCount">The total number of items in the result.</param>   
    /// <param name="requestCharge">The Azure Request Charge that will be applied to the request.</param>
    /// <param name="timeDeltaMs">The time taken for the request in milliseconds.</param>
    public CosmosPagedResult(IEnumerable<T> items, string? continuationToken, int itemsPerPage, int totalCount, double requestCharge, long timeDeltaMs)
        : base(items, requestCharge, timeDeltaMs)
    {
        ContinuationToken = continuationToken;
        ItemsPerPage = itemsPerPage;
        TotalCount = totalCount;
    }

    /// <inheritdoc />
    public string? ContinuationToken { get; }

    /// <inheritdoc />
    public int ItemsPerPage { get; }

    /// <inheritdoc />
    public override int TotalCount { get; }

    /// <inheritdoc />
    public bool IsLastPage => ContinuationToken == null;
    
    /// <inheritdoc />
    public bool IsFirstPage => ContinuationToken == null && TotalCount <= ItemsPerPage;
    
    /// <inheritdoc />
    public int NumberOfPages => (int)Math.Ceiling(TotalCount / (double)ItemsPerPage);
}