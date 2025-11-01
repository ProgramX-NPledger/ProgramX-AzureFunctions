using ProgramX.Azure.FunctionApp.Contract;

namespace ProgramX.Azure.FunctionApp.Cosmos;

/// <summary>
/// Represents a result from Cosmos DB.
/// </summary>
/// <typeparam name="T">Type of item in the result.</typeparam>
public class CosmosResult<T> : IResult<T>, IChargeableResult
{
    protected CosmosResult()
    {
    }

    /// <summary>
    /// Creates a new instance of CosmosResult.
    /// </summary>
    /// <param name="items">Items to return.</param>
    /// <param name="requestCharge">The Azure Request Charge that will be applied to the request.</param>
    /// <param name="timeDeltaMs">The time taken for the request in milliseconds.</param>
    public CosmosResult(IEnumerable<T> items, double requestCharge, double timeDeltaMs)
    {
        Items = items;
        RequestCharge = requestCharge;
        TimeDeltaMs = timeDeltaMs;
    }
    
    /// <inheritdoc />
    public IEnumerable<T> Items { get; }

    /// <inheritdoc />
    public bool IsRequiredToBeOrderedByClient { get; set; } = false;

    /// <summary>
    /// The Azure Request Charge that will be applied to the request.
    /// </summary>
    public double RequestCharge { get; private set; }
    
    /// <inheritdoc />
    public virtual int TotalCount => Items.Count();

    /// <inheritdoc />
    public double TimeDeltaMs { get; private set; }
    
}