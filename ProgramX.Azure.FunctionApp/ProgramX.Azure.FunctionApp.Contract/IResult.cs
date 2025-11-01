namespace ProgramX.Azure.FunctionApp.Contract;

public interface IResult<out T>
{
    /// <summary>
    /// The total number of items in the result.
    /// </summary>
    int TotalCount { get; }
    
    /// <summary>
    /// The items returned by the query.
    /// </summary>
    IEnumerable<T> Items { get; }

    /// <summary>
    /// If the result being returned is required to be ordered by the client.
    /// It is sometimes not possible to order results if they are not in outermost
    /// models.
    /// </summary>
    public bool IsRequiredToBeOrderedByClient { get; set; }
    
    /// <summary>
    /// The time taken for the request in milliseconds.
    /// </summary>
    double TimeDeltaMs { get; }
}