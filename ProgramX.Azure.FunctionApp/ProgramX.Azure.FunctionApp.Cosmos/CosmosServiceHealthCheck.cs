using System.Collections.Specialized;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Exceptions;

namespace ProgramX.Azure.FunctionApp.Cosmos;

public class CosmosServiceHealthCheck(ILoggerFactory loggerFactory) : IServiceHealthCheck
{
    public async Task<ServiceHealthCheckResult> CheckHealthAsync()
    {
        var logger = loggerFactory.CreateLogger<CosmosServiceHealthCheck>();
        using (logger.BeginScope("Health Check {HealthCheckName}", nameof(CosmosServiceHealthCheck)))
        {
            var result = new ServiceHealthCheckResult()
            {
                HealthCheckName = nameof(CosmosServiceHealthCheck),
                IsHealthy = false,
                FriendlyName = "Cosmos DB",
                Items = new List<ServiceHealthCheckItemResult>()
                {
                    new ServiceHealthCheckItemResult()
                    {
                        FriendlyName = "Connection string",
                        Name = "ConnectionString"
                    },
                    new ServiceHealthCheckItemResult()
                    {
                        FriendlyName = "Connect to Cosmos DB",
                        Name = "CosmosClient"
                    },
                    new ServiceHealthCheckItemResult()
                    {
                        FriendlyName = "Cosmos DB Database",
                        Name = "Database"
                    },
                    new ServiceHealthCheckItemResult()
                    {
                        FriendlyName = "Cosmos DB Container",
                        Name = "Container"
                    },
                    new ServiceHealthCheckItemResult()
                    {
                        FriendlyName = "Write to Container",
                        Name = "WriteToContainer"
                    },
                    new ServiceHealthCheckItemResult()
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

    private async Task CheckReadAsync(ServiceHealthCheckItemResult healthCheckItem, Container container, HealthCheckItemTest healthCheckItemTest)
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


    private async Task<HealthCheckItemTest> CheckWriteAsync(ServiceHealthCheckItemResult healthCheckItem, Container container)
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
    
    
    private async Task<Container> CheckAndGetContainerAsync(ServiceHealthCheckItemResult healthCheckItem, Database database)
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

    
    
    private async Task<Database> CheckAndGetDatabaseAsync(ServiceHealthCheckItemResult healthCheckItem, CosmosClient cosmosClient)
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

    
    private CosmosClient? CheckAndGetCosmosClient(ServiceHealthCheckItemResult healthCheckItem, string connectionString)
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

    private string CheckAndGetEnvironmentVariable(ServiceHealthCheckItemResult currentHealthCheck)
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