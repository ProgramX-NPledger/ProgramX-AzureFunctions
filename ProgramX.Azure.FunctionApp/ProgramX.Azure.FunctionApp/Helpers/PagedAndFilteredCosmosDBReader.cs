using Microsoft.Azure.Cosmos;

namespace ProgramX.Azure.FunctionApp.Helpers;

public class PagedAndFilteredCosmosDBReader<T>
{
    private readonly CosmosClient _client;
    private readonly string _databaseName;
    private readonly string _containerName;

    public PagedAndFilteredCosmosDBReader(CosmosClient client, string databaseName, string containerName)
    {
        _client = client;
        _databaseName = databaseName;
        _containerName = containerName;
    }

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