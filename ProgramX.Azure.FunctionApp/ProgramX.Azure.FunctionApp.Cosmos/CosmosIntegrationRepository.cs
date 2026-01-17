using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using User = Microsoft.Azure.Cosmos.User;

namespace ProgramX.Azure.FunctionApp.Cosmos;

public class CosmosIntegrationRepository : IIntegrationRepository
{
    private readonly ILogger<CosmosIntegrationRepository> _logger;
    private readonly CosmosClient _cosmosClient;

    public CosmosIntegrationRepository( 
        CosmosClient cosmosClient,
        ILogger<CosmosIntegrationRepository> logger)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
    }
    
    /// <inheritdoc />
    public async Task<IntegrationCredentials?> GetIntegrationCredentialsForServiceAsync(string serviceName)
    {
        using (_logger.BeginScope("{methodName} {serviceName}", nameof(GetIntegrationCredentialsForServiceAsync),serviceName))
        {
            QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.serviceName=@serviceName");
            queryDefinition.WithParameter("@serviceName", serviceName);
            
            _logger.LogDebug("QueryDefinition: {queryDefinition}", queryDefinition);
            
            CosmosReader<IntegrationCredentials> cosmosReader = new CosmosReader<IntegrationCredentials>(_cosmosClient,
                DatabaseNames.Core,
                ContainerNames.Integrations,
                ContainerNames.IntegrationNamePartitionKey);
            
            IResult<IntegrationCredentials> result = await cosmosReader.GetItemsAsync(queryDefinition);
            if (!result.Items.Any())
            {
                _logger.LogError("No IntegrationCredentials found for serviceName {serviceName}", serviceName);

                return null;
            }

            return result.Items.Single();
        }
    }

    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the update failed.</exception>
    public async Task SetBearerAndRefreshTokensAsync(string serviceName, string clientId, string bearerToken, string refreshToken)
    {
        using (_logger.BeginScope("{methodName} {serviceName}", nameof(SetBearerAndRefreshTokensAsync), serviceName))
        {
            var container = _cosmosClient.GetContainer(DatabaseNames.Core, ContainerNames.Integrations);
            var response = await container.CreateItemAsync(new IntegrationCredentials()
            {
                serviceName = serviceName,
                clientId = clientId,
                bearerToken = bearerToken,
                refreshToken = refreshToken,
                lastUpdatedAt = DateTime.Now
            }, new PartitionKey(serviceName));

            if (response.StatusCode != HttpStatusCode.Created)
            {
                _logger.LogError(
                    "Failed to create integration crednetials for service {serviceName} with status code {statusCode} and response {response}", 
                    serviceName,
                    response.StatusCode, 
                    response);
                throw new RepositoryException(OperationType.Create, typeof(IntegrationCredentials));
            }
            _logger.LogDebug("Success");
        }    
        
    }
}