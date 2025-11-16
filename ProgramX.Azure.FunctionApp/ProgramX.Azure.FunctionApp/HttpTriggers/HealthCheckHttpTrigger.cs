using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Cosmos;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class HealthCheckHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<HealthCheckHttpTrigger> _logger;
    private readonly ISingletonMutex _singletonMutex;

    public HealthCheckHttpTrigger(ILoggerFactory loggerFactory,
        IConfiguration configuration,
        ISingletonMutex singletonMutex) : base(configuration)
    {
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<HealthCheckHttpTrigger>();
        _singletonMutex = singletonMutex;
    }
    
    [Function(nameof(GetHealthCheck))]
    public async Task<HttpResponseData> GetHealthCheck(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "healthcheck/{name?}")] HttpRequestData req, string? name)
    {
        if (!_singletonMutex.IsRequestWithinSecondsOfMostRecentRequestOfSameType(name)) // throttle health checks to once every minute
        {
            _singletonMutex.RegisterHealthCheckForType(name ?? string.Empty);

            if (string.IsNullOrWhiteSpace(name))
            {
                return await HttpResponseDataFactory.CreateForSuccess(req, new GetHealthCheckResponse()
                {
                    TimeStamp = DateTime.UtcNow,
                });
            }
            else
            {
                return await PerformSpecificHealthCheck(req, name);       
            }
        }
        else
        {
            return await HttpResponseDataFactory.CreateForTooManyRequests(req);
        }
    }

    private async Task<HttpResponseData> PerformSpecificHealthCheck(HttpRequestData httpRequestData, string name)
    {
        // return the health check item for the specified name

        IHealthCheck? healthCheck = GetHealthCheckByName(name);
        if (healthCheck != null)
        {
            var healthCheckResult = await healthCheck.CheckHealthAsync();
            
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new HealthCheckItemResponse()
            {
                Name = name,
                IsHealthy = healthCheckResult.IsHealthy, 
                Message = healthCheckResult.Message,
                TimeStamp = DateTime.UtcNow,
                SubItems = healthCheckResult.Items ?? new List<HealthCheckItemResult>()
            });
            
        }
        else
        {
            return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, $"No health check found for {name}");
        }
        
        
    }

    private IHealthCheck? GetHealthCheckByName(string name)
    {
        switch (name)
        {
            // TODO: More health checks here, using name in GetHealthCheckResponse
            case "azure-cosmos-db": return new CosmosHealthCheck(_loggerFactory);
            default: return null;
        }
    }
}