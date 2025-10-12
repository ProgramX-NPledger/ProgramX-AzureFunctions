using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Helpers;

public class PagedCosmosDbResult<T>
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
    
    public double RequestCharge { get; private set; }

    public int TotalItems { get; private set;  }
    
    public double TimeDeltaMs { get; private set; }

    public PagedCosmosDbResult(IEnumerable<T> items, string? continuationToken, int? maximumItemsRequested, double requestCharge, int totalItems, double timeDeltaMs)
    {
        Items = items;
        ContinuationToken = continuationToken;
        MaximumItemsRequested = maximumItemsRequested;
        RequestCharge = requestCharge;
        TotalItems = totalItems;
        TimeDeltaMs = timeDeltaMs;
    }
    
    /// <summary>
    /// Creates a new <seealso cref="PagedCosmosDbResult{T}"/> by transforming the items to a different type.
    /// </summary>
    /// <remarks>This is useful when a child-model needs to be "brought up" into a higher model for paging purposes.</remarks>
    /// <param name="transformDelegate">Delegate called per item in <see cref="Items"/>.</param>
    /// <param name="disambiguationDelegate">Delegate called to ensure uniqueness of items in resulting collection.</param>
    /// <typeparam name="TTargetType">Type of the transformed type.</typeparam>
    /// <returns>A new <see cref="PagedCosmosDbResult{T}"/> with the same metrics but a transformed Items collection.</returns>
    public PagedCosmosDbResult<TTargetType> TransformItemsToDifferentType<TTargetType>(Func<T, IEnumerable<TTargetType>> transformDelegate,
        Func<TTargetType,IEnumerable<TTargetType>, bool> disambiguationDelegate
        )
    {
        List<TTargetType> transformedItems = new List<TTargetType>();
        foreach (var item in Items)
        {
            // check if item not already added
            if (!transformedItems.Any(x => disambiguationDelegate.Invoke(x,transformedItems)))
            {
                transformedItems.AddRange(transformDelegate.Invoke(item));    
            }
        }
        return new PagedCosmosDbResult<TTargetType>(transformedItems, ContinuationToken, MaximumItemsRequested, RequestCharge, TotalItems, TimeDeltaMs);
    }

    public void OrderItemsBy<TKey>(
        [NotNull] Func<T, TKey> keySelector)
    {
        Items = Items.OrderBy(keySelector);
    }
    
    public void OrderItemsByDescending<TKey>(
        [NotNull] Func<T, TKey> keySelector)
    {
        Items = Items.OrderByDescending(keySelector);
    }
}