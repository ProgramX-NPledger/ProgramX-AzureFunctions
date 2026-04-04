using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.AzureStorage.Constants;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Exceptions;

namespace ProgramX.Azure.FunctionApp.AzureStorage;

public class AzureStorageHealthCheck(ILoggerFactory loggerFactory) : IHealthCheck
{
    
    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        var logger = loggerFactory.CreateLogger<AzureStorageHealthCheck>();
        using (logger.BeginScope("Health Check {HealthCheckName}", nameof(AzureStorageHealthCheck)))
        {
            var result = new HealthCheckResult()
            {
                HealthCheckName = nameof(AzureStorageHealthCheck),
                IsHealthy = false,
                Items = new List<HealthCheckItemResult>()
                {
                    new HealthCheckItemResult()
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
    
    private string CheckAndGetEnvironmentVariable(HealthCheckItemResult currentHealthCheck)
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