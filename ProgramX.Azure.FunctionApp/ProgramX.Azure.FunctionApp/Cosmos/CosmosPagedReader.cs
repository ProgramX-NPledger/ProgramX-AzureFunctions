using System.Diagnostics;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model.Constants;

namespace ProgramX.Azure.FunctionApp.Cosmos;

public class CosmosPagedReader<T> : CosmosReader<T>
{
    private readonly ILogger<CosmosPagedReader<T>> _logger;
    
    /// <summary>
    /// Constructor to initialise the reader.
    /// </summary>
    /// <param name="client">CosmosDB client.</param>
    /// <param name="databaseName">Name of the database. The database will be created if it doesn't already exist.</param>
    /// <param name="containerName">Name of the container. The container will be created if it doesn't already exist.</param>
    /// <param name="partitionKeyPath">Partition Key path used by CosmosDB for indexing.</param>
    public CosmosPagedReader(CosmosClient client, string databaseName, string containerName, string partitionKeyPath) : 
        base(client, databaseName, containerName, partitionKeyPath)
    {
        _logger = new LoggerFactory().CreateLogger<CosmosPagedReader<T>>();
    }

    /// <summary>
    /// Returns a paged, strongly typed result from Cosmos DB using Continuation Tokens for forward-only efficiency.
    /// </summary>
    /// <param name="queryDefinition">The <seealso cref="QueryDefinition"/> representing the query to execute.</param>
    /// <param name="continuationToken">The Continuation Token is used to access the next page. Use <c>null</c> for final or only page.</param>
    /// <param name="itemsPerPage">Optional. The number of items per page. Use <c>null</c> to request number of items specified by <see cref="DataConstants.ItemsPerPage"/>.</param>
    /// <returns>A <seealso cref="PagedCosmosDbResult{T}"/> containing the strongly typed result and the Continuation Token if further pages are available.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the provided container name is invalid.</exception>
    public async Task<CosmosPagedResult<T>> GetNextItemsAsync(QueryDefinition queryDefinition,
        string? continuationToken = null,
        int? itemsPerPage = PagingConstants.ItemsPerPage)
    {
        return await GetNextItemsAsyncImpl(queryDefinition, continuationToken, null, itemsPerPage, null);   
    }
    
    
    /// <summary>
    /// Returns a paged, strongly typed result from Cosmos DB.
    /// </summary>
    /// <param name="queryDefinition">The <seealso cref="QueryDefinition"/> representing the query to execute.</param>
    /// <param name="offset">Optional. The offset to start from. If not set, assumes the top.</param>   
    /// <param name="itemsPerPage">Optional. The number of items per page. If not set, assumes the number of items defined by <see cref="DataConstants.ItemsPerPage"/>.</param>
    /// <returns>A <seealso cref="CosmosPagedResult{T}"/> containing the strongly typed result.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the provided container name is invalid.</exception>
    public async Task<CosmosPagedResult<T>> GetPagedItemsAsync(QueryDefinition queryDefinition,
        int? offset=0,
        int? itemsPerPage = PagingConstants.ItemsPerPage)
    {
        return await GetNextItemsAsyncImpl(queryDefinition, null, offset, itemsPerPage, null);   
    }

    /// <summary>
    /// Returns a paged, strongly typed result from CosmosDB ordered by the specified field.
    /// </summary>
    /// <param name="queryDefinition">The <seealso cref="QueryDefinition"/> representing the query to execute.</param>
    /// <param name="orderByField">The field to sort the results by. This field must not have the container specified.</param>
    /// <param name="offset">Optional. The offset to start from. If not set, assumes the top.</param>   
    /// <param name="itemsPerPage">Optional. The number of items per page. If not set, assumes the number of items defined by <see cref="DataConstants.ItemsPerPage"/>.</param>
    /// <returns>A <seealso cref="CosmosPagedResult{T}"/> containing the strongly typed result.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the provided container name is invalid.</exception>
    /// <remarks>It is not possible to sort by a field that is not in the outermost model.</remarks>
    public async Task<CosmosPagedResult<T>> GetOrderedPagedItemsAsync(QueryDefinition queryDefinition,
        string orderByField,
        int offset = 0,
        int itemsPerPage = PagingConstants.ItemsPerPage)
    {
        return await GetNextItemsAsyncImpl(queryDefinition, null, offset, itemsPerPage, orderByField);   
    }

    
    

    protected virtual async Task<CosmosPagedResult<T>> GetNextItemsAsyncImpl(QueryDefinition queryDefinition,
        string? continuationToken = null,
        int? offset = 0,
        int? itemsPerPage = PagingConstants.ItemsPerPage,
        string? orderByField = null)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        
        var usesIndexedPages = itemsPerPage != null && offset != null;
        
        List<T> items = new List<T>();
        
        var container = await PrepareAndGetContainerAsync();

        var requestCharge = 0.0;
        using (var feedIterator = container.GetItemQueryIterator<T>(queryDefinition, continuationToken,
                   new QueryRequestOptions
                   {
                       MaxItemCount = itemsPerPage,
                   }))
        {
            var response = await feedIterator.ReadNextAsync();
            continuationToken = response.ContinuationToken;
            requestCharge += response.RequestCharge;
                
            items.AddRange(response);
        }

        var countQueryDefinition = BuildTotalItemsCountQueryDefinition(queryDefinition);
        var totalCount = await GetCountAsync(container, countQueryDefinition);
        
        stopwatch.Stop();
        
        return new CosmosPagedResult<T>(items,
            continuationToken, 
            itemsPerPage ?? PagingConstants.ItemsPerPage,
            totalCount,
            requestCharge,
            (long)stopwatch.Elapsed.TotalMilliseconds);
    }


    private QueryDefinition BuildTotalItemsCountQueryDefinition(QueryDefinition queryDefinition)
    {
        var countQueryDefinition = new QueryDefinition("SELECT VALUE COUNT(1) " +
                                                       queryDefinition.QueryText.Substring(
                                                           queryDefinition.QueryText.IndexOf("FROM",
                                                               StringComparison.InvariantCultureIgnoreCase)));
        foreach (var parameter in queryDefinition.GetQueryParameters())
        {
            countQueryDefinition.WithParameter(parameter.Name,parameter.Value);
        }
        return countQueryDefinition;
    }



    private QueryDefinition BuildPagedQueryDefinition(QueryDefinition queryDefinition, 
        string? orderBy, int offset, int itemsPerPage)
    {
        var sql = queryDefinition.QueryText;
        // add order by
        var splitSqlOnOrderBy = queryDefinition.QueryText.Split("ORDER BY");
        if (splitSqlOnOrderBy.Length <= 1 && !string.IsNullOrEmpty(orderBy))
        {
            string containerAlias;
            if (orderBy.Contains("."))
            {
                // looks like the alias is already in the order by
                sql += $" ORDER BY {orderBy}";
            }
            else
            {
                // get the container alias name
                var containerAliasSplit = queryDefinition.QueryText.Split("FROM ");
                containerAlias = containerAliasSplit[0];
                sql += $" ORDER BY {containerAlias}.{orderBy}";
            }
        }
        
        // ensure offset is not already in query SQL
        var splitSqlOnOffset = queryDefinition.QueryText.Split("OFFSET");
        if (splitSqlOnOffset.Length <= 1)
        {
            // add it to query SQL
            sql += $" OFFSET @offset LIMIT @itemsPerPage";
        }
        
        // create new QueryDefinition SQL and populate parameters
        var pagedQueryDefinition = new QueryDefinition(sql);
        foreach (var parameter in queryDefinition.GetQueryParameters())
        {
            pagedQueryDefinition.WithParameter(parameter.Name, parameter.Value);       
        }
        pagedQueryDefinition.WithParameter("@offset", offset);
        pagedQueryDefinition.WithParameter("@itemsPerPage", itemsPerPage);
        return pagedQueryDefinition;
    }

 
    private async Task<int> GetCountAsync(Container container, QueryDefinition queryDefinition)
    {
        using var iterator = container.GetItemQueryIterator<int>(queryDefinition);
        var response = await iterator.ReadNextAsync();
        return response.FirstOrDefault();
    }

}