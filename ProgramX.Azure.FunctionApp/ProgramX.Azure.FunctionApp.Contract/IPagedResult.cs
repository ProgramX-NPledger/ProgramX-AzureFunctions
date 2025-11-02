namespace ProgramX.Azure.FunctionApp.Contract;

public interface IPagedResult<out T> : IResult<T>
{
    
    /// <summary>
    /// The continuation token to use for the next page.
    /// </summary>
    string? ContinuationToken { get; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    int ItemsPerPage { get; }
    
    /// <summary>
    /// Returns true if this is the first page of results.
    /// </summary>
    bool IsFirstPage { get; }

    /// <summary>
    /// The number of pages of results.
    /// </summary>
    int NumberOfPages { get; }
}