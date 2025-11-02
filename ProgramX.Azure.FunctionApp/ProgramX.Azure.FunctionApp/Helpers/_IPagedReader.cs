using Microsoft.Azure.Cosmos;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Model.Constants;

namespace ProgramX.Azure.FunctionApp.Helpers;

public interface _IPagedReader<T>
{
    /// <summary>
    /// Returns a paged, strongly typed result from CosmosDB using Continuation Tokens for forward-only efficiency.
    /// </summary>
    /// <param name="queryDefinition">The <seealso cref="QueryDefinition"/> representing the query to execute.</param>
    /// <param name="continuationToken">Optional. The optional Continuation Token is used to access the next page. Use <c>null</c> for final or only page.</param>
    /// <param name="itemsPerPage">Optional. The number of items per page. Use <c>null</c> to request an unpaged result.</param>
    /// <returns>A <seealso cref="PagedCosmosDbResult{T}"/> containing the strongly typed result and the Continuation Token if further pages are available.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the provided container name is invalid.</exception>
    Task<PagedCosmosDbResult<T>> GetNextItemsAsync(QueryDefinition queryDefinition,
        string? continuationToken = null,
        int? itemsPerPage = null);

    /// <summary>
    /// Returns a paged, strongly typed result from CosmosDB.
    /// </summary>
    /// <param name="queryDefinition">The <seealso cref="QueryDefinition"/> representing the query to execute.</param>
    /// <param name="orderByField">The field to order by.</param>   
    /// <param name="offset">Optional. The offset to start from. Use <c>null</c> to start from the beginning.</param>   
    /// <param name="itemsPerPage">Optional. The number of items per page. Use <c>null</c> to request an unpaged result.</param>
    /// <returns>A <seealso cref="PagedCosmosDbResult{T}"/> containing the strongly typed result.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the provided container name is invalid.</exception>
    Task<PagedCosmosDbResult<T>> GetPagedItemsAsync(QueryDefinition queryDefinition,
        string? orderByField,
        int? offset=0,
        int? itemsPerPage = PagingConstants.ItemsPerPage);
}