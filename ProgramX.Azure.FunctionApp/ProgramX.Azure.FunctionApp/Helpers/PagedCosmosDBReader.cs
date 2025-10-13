using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Constants;

namespace ProgramX.Azure.FunctionApp.Helpers;

/// <summary>
/// Provides paging query functionality for CosmosDB.
/// </summary>
/// <typeparam name="T">Type of model to query.</typeparam>
public class PagedCosmosDbReader<T>
{
    private readonly CosmosClient _client;
    private readonly string _databaseName;
    private readonly string _containerName;
    private readonly string _partitionKeyPath;
    private readonly ILogger<PagedCosmosDbReader<T>> _logger;
    
    /// <summary>
    /// Constructor to initialise the reader.
    /// </summary>
    /// <param name="client">CosmosDB client.</param>
    /// <param name="databaseName">Name of the database. The database will be created if it doesn't already exist.</param>
    /// <param name="containerName">Name of the container. The container will be created if it doesn't already exist.</param>
    /// <param name="partitionKeyPath">Partition Key path used by CosmosDB for indexing.</param>
    public PagedCosmosDbReader(CosmosClient client, string databaseName, string containerName, string partitionKeyPath)
    {
        _client = client;
        _databaseName = databaseName;
        _containerName = containerName;
        _partitionKeyPath = partitionKeyPath;
        if (!_partitionKeyPath.StartsWith("/")) _partitionKeyPath = "/" + _partitionKeyPath;
        _logger = new LoggerFactory().CreateLogger<PagedCosmosDbReader<T>>();
    }

    /// <summary>
    /// Returns a paged, strongly typed result from CosmosDB using Continuation Tokens for forward-only efficiency.
    /// </summary>
    /// <param name="queryDefinition">The <seealso cref="QueryDefinition"/> representing the query to execute.</param>
    /// <param name="continuationToken">Optional. The optional Continuation Token is used to access the next page. Use <c>null</c> for final or only page.</param>
    /// <param name="itemsPerPage">Optional. The number of items per page. Use <c>null</c> to request an unpaged result.</param>
    /// <returns>A <seealso cref="PagedCosmosDbResult{T}"/> containing the strongly typed result and the Continuation Token if further pages are available.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the provided container name is invalid.</exception>
    public async Task<PagedCosmosDbResult<T>> GetNextItemsAsync(QueryDefinition queryDefinition,
        string? continuationToken = null,
        int? itemsPerPage = null)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        
        var isPaged = itemsPerPage != null;
        List<T> items = new List<T>();
        
        var containerResponse = await PrepareAndGetContainerAsync();

        var requestCharge = 0.0;
        using (var feedIterator = containerResponse.Container.GetItemQueryIterator<T>(queryDefinition, continuationToken,
                   new QueryRequestOptions
                   {
                       MaxItemCount = itemsPerPage,
                       
                   }))
        {
            if (isPaged)
            {
                var response = await feedIterator.ReadNextAsync();
                continuationToken = response.ContinuationToken;
                requestCharge = response.RequestCharge;
                
                items.AddRange(response);
            }
            else
            {
                while (feedIterator.HasMoreResults)
                {
                    var response = await feedIterator.ReadNextAsync();
                    items.AddRange(response);
                    requestCharge = response.RequestCharge;
                }
                
            }
        }
        
        var countQueryDefinition = new QueryDefinition("SELECT VALUE COUNT(1) "+queryDefinition.QueryText.Substring(queryDefinition.QueryText.IndexOf("FROM",StringComparison.InvariantCultureIgnoreCase)));
        foreach (var parameter in queryDefinition.GetQueryParameters())
        {
            countQueryDefinition.WithParameter(parameter.Name,parameter.Value);
        }
        var totalCount = await GetCountAsync(containerResponse.Container, countQueryDefinition);

        stopwatch.Stop();
        var pagedCosmosDbResult = new PagedCosmosDbResult<T>(items,continuationToken,itemsPerPage,requestCharge,totalCount, (long)stopwatch.Elapsed.TotalMilliseconds);
        return pagedCosmosDbResult;
        
        

    }
    
    /// <summary>
    /// Returns a paged, strongly typed result from CosmosDB.
    /// </summary>
    /// <param name="queryDefinition">The <seealso cref="QueryDefinition"/> representing the query to execute.</param>
    /// <param name="orderByField">The field to order by.</param>   
    /// <param name="offset">Optional. The offset to start from. Use <c>null</c> to start from the beginning.</param>   
    /// <param name="itemsPerPage">Optional. The number of items per page. Use <c>null</c> to request an unpaged result.</param>
    /// <returns>A <seealso cref="PagedCosmosDbResult{T}"/> containing the strongly typed result.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the provided container name is invalid.</exception>
    public async Task<PagedCosmosDbResult<T>> GetPagedItemsAsync(QueryDefinition queryDefinition,
        string? orderByField,
        int? offset=0,
        int? itemsPerPage = DataConstants.ItemsPerPage)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        
        List<T> items = new List<T>();

        var containerResponse = await PrepareAndGetContainerAsync();
        var pagedQueryDefinition = BuildPagedQueryDefinition(queryDefinition,
            orderByField,
            offset ?? 0,
            itemsPerPage ?? DataConstants.ItemsPerPage);
        
        double requestCharge;
        using (var feedIterator = containerResponse.Container.GetItemQueryIterator<T>(pagedQueryDefinition, null,
                   new QueryRequestOptions
                   {
                       MaxItemCount = itemsPerPage,
                   }))
        {
            var response = await feedIterator.ReadNextAsync();
            requestCharge = response.RequestCharge;
            items.AddRange(response);
        }
        
        var countQueryDefinition = new QueryDefinition("SELECT VALUE COUNT(1) FROM ("+queryDefinition.QueryText+")");
        foreach (var parameter in queryDefinition.GetQueryParameters())
        {
            countQueryDefinition.WithParameter(parameter.Name,parameter.Value);
        }
        var totalCount = await GetCountAsync(containerResponse.Container, countQueryDefinition);
        stopwatch.Stop();
        
        var pagedCosmosDbResult = new PagedCosmosDbResult<T>(items,null,itemsPerPage,requestCharge,totalCount, stopwatch.Elapsed.TotalMilliseconds);
        return pagedCosmosDbResult;
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
                containerAlias = orderBy.Substring(0, orderBy.IndexOf(".", StringComparison.InvariantCultureIgnoreCase));
            }
            else
            {
                // get the container alias name
                var containerAliasSplit = queryDefinition.QueryText.Split("FROM ");
                containerAlias = containerAliasSplit[0];
            }
            
            sql += $" ORDER BY {containerAlias}.{orderBy}";
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

    private async Task<ContainerResponse> PrepareAndGetContainerAsync()
    {
        var databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
        if (databaseResponse.StatusCode==HttpStatusCode.Created) _logger.LogInformation("Database {_databaseName} created",[_databaseName]);
        var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(_containerName, _partitionKeyPath);
        if (containerResponse.StatusCode==HttpStatusCode.Created) _logger.LogInformation("Container {containerName} created",[_containerName,_partitionKeyPath]);

        return containerResponse;
    }

    private async Task<int> GetCountAsync(Container container, QueryDefinition queryDefinition)
    {
        using var iterator = container.GetItemQueryIterator<int>(queryDefinition);
        var response = await iterator.ReadNextAsync();
        return response.FirstOrDefault();
    }

    
}