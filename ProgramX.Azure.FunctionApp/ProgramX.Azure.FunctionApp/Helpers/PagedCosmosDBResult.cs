namespace ProgramX.Azure.FunctionApp.Helpers;

public class PagedCosmosDBResult<T>
{
    public IEnumerable<T> Items { get; private set; }
    public string? ContinuationToken { get; private set;  }

    public int? MaximumItemsRequested { get; private set; }
    
    public bool IsMorePages()
    {
        return ContinuationToken != null;
    }

    public bool IsConstrainedByPageLength()
    {
        return MaximumItemsRequested != null;
    }

    public PagedCosmosDBResult(IEnumerable<T> items, string? continuationToken, int? maximumItemsRequested)
    {
        Items = items;
        ContinuationToken = continuationToken;
        MaximumItemsRequested = maximumItemsRequested;
    }
}