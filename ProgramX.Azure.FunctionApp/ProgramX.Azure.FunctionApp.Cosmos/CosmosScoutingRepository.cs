using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Cosmos;

public class CosmosScoutingRepository(CosmosClient cosmosClient, ILogger<CosmosScoutingRepository> logger) : IScoutingRepository
{
    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the creation failed.</exception>
    public async Task CreateScoutingActivityAsync(ScoutingActivity scoutingActivity)
    {
        using (logger.BeginScope("CreateScoutingActivityAsync {scoutingActivity}", scoutingActivity))
        {
            var container = cosmosClient.GetContainer(DatabaseNames.Scouting, ContainerNames.ScoutingActivities);
            var response = await container.CreateItemAsync(scoutingActivity, new PartitionKey(scoutingActivity.id));

            if (response.StatusCode != HttpStatusCode.Created)
            {
                logger.LogError(
                    "Failed to create {type} with id {id} with status code {statusCode} and response {response}", nameof(ScoutingActivity),scoutingActivity.id,
                    response.StatusCode, response);
                throw new RepositoryException(OperationType.Create, typeof(ScoutingActivity));
            }
            logger.LogDebug("Success");
        }    
    }
    
    
}