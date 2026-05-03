using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.AzureStorage.Constants;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Exceptions;

namespace ProgramX.Azure.FunctionApp.AzureStorage;

public class AzureStorageServiceHealthCheck(ILoggerFactory loggerFactory) : IServiceHealthCheck
{
    
    public async Task<ServiceHealthCheckResult> CheckHealthAsync()
    {
        var logger = loggerFactory.CreateLogger<AzureStorageServiceHealthCheck>();
        using (logger.BeginScope("Health Check {HealthCheckName}", nameof(AzureStorageServiceHealthCheck)))
        {
            var result = new ServiceHealthCheckResult()
            {
                HealthCheckName = nameof(AzureStorageServiceHealthCheck),
                IsHealthy = false,
                FriendlyName = "Azure Storage",
                Items = new List<ServiceHealthCheckItemResult>()
                {
                    new ServiceHealthCheckItemResult()
                    {
                        FriendlyName = "Connection string",
                        Name = "ConnectionString"
                    }
                }
            };

            try
            {
                _ = CheckAndGetEnvironmentVariable(result.Items.Single(q => q.Name == "ConnectionString"));

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

            return result;
        }
        
      
    }
    
    private string CheckAndGetEnvironmentVariable(ServiceHealthCheckItemResult currentHealthCheck)
    {
        string? connectionString = Environment.GetEnvironmentVariable(ConfigurationConstants.AzureStorageConnectionString);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new HealthCheckException(currentHealthCheck, $"{ConfigurationConstants.AzureStorageConnectionString} environment variable is not set");
        }
        currentHealthCheck.IsHealthy = true;
        currentHealthCheck.Message = "OK";
        return connectionString;
        
    }
}