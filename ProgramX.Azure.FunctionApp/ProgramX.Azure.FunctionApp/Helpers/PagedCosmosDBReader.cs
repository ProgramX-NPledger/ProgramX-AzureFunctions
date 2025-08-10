using Microsoft.Azure.Cosmos;

namespace ProgramX.Azure.FunctionApp.Helpers;

public class PagedCosmosDBReader<T>
{
    private readonly CosmosClient _client;
    private readonly string _databaseName;
    private readonly string _containerName;

    public PagedCosmosDBReader(CosmosClient client, string databaseName, string containerName)
    {
        _client = client;
        _databaseName = databaseName;
        _containerName = containerName;
    }

    /// <summary>
    /// Returns a paged, strongly typed result from CosmosDB.
    /// </summary>
    /// <param name="queryDefinition">The <seealso cref="QueryDefinition"/> representing the query to execute.</param>
    /// <param name="continuationToken">Optional. The optional continuation token used to access the next page. Use <c>null</c> for final or only page.</param>
    /// <param name="itemsPerPage">Optional. The number of items per page. Use <c>null</c> to request an unpaged result.</param>
    /// <returns>A <seealso cref="PagedCosmosDBResult{T}"/> containing the strongly typed result and the continuation token, if subsequent pages are available.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the provided container name is invalid.</exception>
    public async Task<PagedCosmosDBResult<T>> GetItems(QueryDefinition queryDefinition,
        string? continuationToken = null,
        int? itemsPerPage = null)
    {
        var isPaged = itemsPerPage != null;
        List<T> items = new List<T>();
        var container = _client.GetContainer(_databaseName, _containerName);
        if (container == null) throw new InvalidOperationException("Container not found");

        using (var feedIterator = container.GetItemQueryIterator<T>(queryDefinition, continuationToken,
                   new QueryRequestOptions
                   {
                       MaxItemCount = itemsPerPage
                   }))
        {
            if (isPaged)
            {
                var response = await feedIterator.ReadNextAsync();
                continuationToken = response.ContinuationToken;
                items.AddRange(response);
            }
            else
            {
                while (feedIterator.HasMoreResults)
                {
                    var response = await feedIterator.ReadNextAsync();
                    items.AddRange(response);
                }
                
            }
        }
        var pagedCosmosDbResult = new PagedCosmosDBResult<T>(items,continuationToken,itemsPerPage);
        return pagedCosmosDbResult;
        
        // {"OptimisticDirectExecutionToken":{"token":"-RID:~fahmANNVhRoHAAAAAAAAAA==#RT:1#TRC:5#ISV:2#IEO:65567#QCF:8","range":{"min":"","max":"FF"}}}

    }
    
}