using System.Collections.Specialized;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Cosmos;

public class CosmosHealthCheck(ILoggerFactory loggerFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        var logger = loggerFactory.CreateLogger<CosmosHealthCheck>();
        using (logger.BeginScope("Health Check {HealthCheckName}", nameof(CosmosHealthCheck)))
        {
            var result = new HealthCheckResult()
            {
                HealthCheckName = nameof(CosmosHealthCheck),
                IsHealthy = false,
                Items = new List<HealthCheckItemResult>()
                {
                    new HealthCheckItemResult()
                    {
                        FriendlyName = "Connection string",
                        Name = "ConnectionString"
                    },
                    new HealthCheckItemResult()
                    {
                        FriendlyName = "Connect to Cosmos DB",
                        Name = "CosmosClient"
                    },
                    new HealthCheckItemResult()
                    {
                        FriendlyName = "Cosmos DB Database",
                        Name = "Database"
                    },
                    new HealthCheckItemResult()
                    {
                        FriendlyName = "Cosmos DB Container",
                        Name = "Container"
                    },
                    new HealthCheckItemResult()
                    {
                        FriendlyName = "Write to Container",
                        Name = "WriteToContainer"
                    },
                    new HealthCheckItemResult()
                    {
                        FriendlyName = "Read from Container",
                        Name = "ReadFromContainer"
                    }
                }
            };

            CosmosClient? cosmosClient = null;
            Container? container = null;
            Database? database = null;
            HealthCheckItemTest? healthCheckItemTest = null;

            string connectionString;
            try
            {
                connectionString =
                    CheckAndGetEnvironmentVariable(result.Items.Single(q => q.Name == "ConnectionString"));
                cosmosClient =
                    CheckAndGetCosmosClient(result.Items.Single(q => q.Name == "CosmosClient"), connectionString);
                database = await CheckAndGetDatabaseAsync(result.Items.Single(q => q.Name == "Database"),
                    cosmosClient!);
                container = await CheckAndGetContainerAsync(result.Items.Single(q => q.Name == "Container"), database!);
                healthCheckItemTest =
                    await CheckWriteAsync(result.Items.Single(q => q.Name == "WriteToContainer"), container!);
                await CheckReadAsync(result.Items.Single(q => q.Name == "ReadFromContainer"), container!,
                    healthCheckItemTest!);

                result.IsHealthy = true;
            }
            catch (HealthCheckException healthCheckException)
            {
                healthCheckException.CurrentHealthCheck.IsHealthy = false;
                healthCheckException.CurrentHealthCheck.Message = healthCheckException.Message;

                result.IsHealthy = false;
                result.Message = "A critical error occurred. Check the logs for more details.";

                logger.LogCritical(healthCheckException,
                    "Health check failed at {HealthCheckItem}: {HealthCheckItemMessage}",
                    healthCheckException.CurrentHealthCheck.Name,
                    healthCheckException.Message);
            }
            finally
            {
                if (cosmosClient != null) cosmosClient.Dispose();
            }

            return result;
        }
    

    }

    public async Task<IEnumerable<string>> Fix()
    {
        throw new NotSupportedException("Cosmos health check cannot be fixed automatically");
    }

    private async Task CheckReadAsync(HealthCheckItemResult healthCheckItem, Container container, HealthCheckItemTest healthCheckItemTest)
    {
        var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.id = @id");
        queryDefinition.WithParameter("@id", healthCheckItemTest.id);
        
        using (var feedIterator = container.GetItemQueryIterator<HealthCheckItemTest>(queryDefinition, null,
                   new QueryRequestOptions()
               ))
        {
            var response = await feedIterator.ReadNextAsync();
            if (response.Count == 0)
            {
                throw new HealthCheckException(healthCheckItem, "Could not read from container");
            }
        }
        
        healthCheckItem.IsHealthy = true;
        healthCheckItem.Message = "OK";
    }


    private async Task<HealthCheckItemTest> CheckWriteAsync(HealthCheckItemResult healthCheckItem, Container container)
    {
        var healthCheckItemTest = new HealthCheckItemTest()
        {
            id = Guid.NewGuid().ToString(),
            timeStamp = DateTime.UtcNow
        };
        
        var response = await container.CreateItemAsync(healthCheckItemTest, new PartitionKey(healthCheckItemTest.id));

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new HealthCheckException(healthCheckItem, "Could not write to container");
        }
        
        healthCheckItem.IsHealthy = true;
        healthCheckItem.Message = "OK";
        
        return healthCheckItemTest;
    }
    
    
    private async Task<Container> CheckAndGetContainerAsync(HealthCheckItemResult healthCheckItem, Database database)
    {
        try
        {
            var containerResponse = await database.CreateContainerIfNotExistsAsync(ContainerNames.HealthChecks, "/id");
            switch (containerResponse.StatusCode)
            {
                case HttpStatusCode.Created:
                    healthCheckItem.IsHealthy = true;
                    healthCheckItem.Message = $"Container created.";
                    return containerResponse.Container;
                case HttpStatusCode.OK:
                    healthCheckItem.IsHealthy = true;
                    healthCheckItem.Message = $"Container exists.";
                    return containerResponse.Container;
                default:
                    throw new HealthCheckException(healthCheckItem, "Could not get Container.");
            }
        }
        catch (Exception e)
        {
            throw new HealthCheckException(healthCheckItem, "Could not get Container due to a Cosmos DB error",e);
        }
    }

    
    
    private async Task<Database> CheckAndGetDatabaseAsync(HealthCheckItemResult healthCheckItem, CosmosClient cosmosClient)
    {
        try
        {
            var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseNames.Core);
            switch (databaseResponse.StatusCode)
            {
                case HttpStatusCode.Created:
                    healthCheckItem.IsHealthy = true;
                    healthCheckItem.Message = $"Database created.";
                    return databaseResponse.Database;
                case HttpStatusCode.OK:
                    healthCheckItem.IsHealthy = true;
                    healthCheckItem.Message = $"Database exists.";
                    return databaseResponse.Database;
                default:
                    throw new HealthCheckException(healthCheckItem, "Could not get Database.");
            }
        }
        catch (Exception e)
        {
            throw new HealthCheckException(healthCheckItem, "Could not get Database due to a Cosmos DB error", e);
        }
    }

    
    private CosmosClient? CheckAndGetCosmosClient(HealthCheckItemResult healthCheckItem, string connectionString)
    {
        try
        {
            healthCheckItem.IsHealthy = true;
            healthCheckItem.Message = "OK";
            return new CosmosClient(connectionString);
        }
        catch (Exception e)
        {
            throw new HealthCheckException(healthCheckItem, "Could not connect to Cosmos DB.",e);
        }
    }

    private string CheckAndGetEnvironmentVariable(HealthCheckItemResult currentHealthCheck)
    {
        string? connectionString = Environment.GetEnvironmentVariable("CosmosDBConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new HealthCheckException(currentHealthCheck, "CosmosDBConnection environment variable is not set");
        }
        currentHealthCheck.IsHealthy = true;
        currentHealthCheck.Message = "OK";
        return connectionString;
        
    }

}