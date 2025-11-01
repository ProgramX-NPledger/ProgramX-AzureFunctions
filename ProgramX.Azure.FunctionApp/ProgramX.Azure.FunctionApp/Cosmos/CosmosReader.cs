using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Helpers;

namespace ProgramX.Azure.FunctionApp.Cosmos;

/// <summary>
/// Provides CosmosDB query functionality.
/// </summary>
/// <typeparam name="T">Type of item to retrieve from Cosmos DB.</typeparam>
public class CosmosReader<T>
{
    private readonly CosmosClient _client;
    private readonly string _databaseName;
    private readonly string _containerName;
    private readonly string _partitionKeyPath;
    private readonly ILogger<CosmosReader<T>> _logger;
    
    /// <summary>
    /// Constructor to initialise the reader.
    /// </summary>
    /// <param name="client">CosmosDB client.</param>
    /// <param name="databaseName">Name of the database. The database will be created if it doesn't already exist.</param>
    /// <param name="containerName">Name of the container. The container will be created if it doesn't already exist.</param>
    /// <param name="partitionKeyPath">Partition Key path used by CosmosDB for indexing.</param>
    public CosmosReader(CosmosClient client, string databaseName, string containerName, string partitionKeyPath)
    {
        _client = client;
        _databaseName = databaseName;
        _containerName = containerName;
        _partitionKeyPath = partitionKeyPath;
        if (!_partitionKeyPath.StartsWith("/")) _partitionKeyPath = "/" + _partitionKeyPath;
        _logger = new LoggerFactory().CreateLogger<CosmosReader<T>>();
    }

    /// <summary>
    /// Returns a paged, strongly typed result from CosmosDB using Continuation Tokens for forward-only efficiency.
    /// </summary>
    /// <param name="queryDefinition">The <seealso cref="QueryDefinition"/> representing the query to execute.</param>
    /// <returns>A <seealso cref="CosmosResult{T}"/> containing the strongly typed result.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the provided container name is invalid.</exception>
    public async Task<CosmosResult<T>> GetItemsAsync(QueryDefinition queryDefinition)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        
        List<T> items = new List<T>();
        
        var container = await PrepareAndGetContainerAsync();

        var requestCharge = 0.0;
        using (var feedIterator = container.GetItemQueryIterator<T>(queryDefinition, null,
                   new QueryRequestOptions()
                   ))
        {
            while (feedIterator.HasMoreResults)
            {
                var response = await feedIterator.ReadNextAsync();
                items.AddRange(response);
                requestCharge += response.RequestCharge;
            }
        }
        
        stopwatch.Stop();

        var pagedCosmosDbResult = new CosmosResult<T>(items,requestCharge, (long)stopwatch.Elapsed.TotalMilliseconds);
        return pagedCosmosDbResult;
    }
    
    /// <summary>
    /// If the database and container don't exist, create them.
    /// </summary>
    /// <returns>The created or existing container.</returns>
    protected async Task<Container> PrepareAndGetContainerAsync()
    {
        var databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(_databaseName,ThroughputProperties.CreateManualThroughput(100),new RequestOptions(),default(CancellationToken));
        if (databaseResponse.StatusCode==HttpStatusCode.Created) _logger.LogInformation("Database {_databaseName} created",[_databaseName]);
        var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(_containerName, _partitionKeyPath,null,new RequestOptions(),default(CancellationToken));
        if (containerResponse.StatusCode==HttpStatusCode.Created) _logger.LogInformation("Container {containerName} created",[_containerName,_partitionKeyPath]);
        return containerResponse.Container;
    }


}