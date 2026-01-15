using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Cosmos;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class HealthCheckHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<HealthCheckHttpTrigger> _logger;
    private readonly ISingletonMutex _singletonMutex;

    public HealthCheckHttpTrigger(ILoggerFactory loggerFactory,
        ILogger<HealthCheckHttpTrigger> logger,
        IConfiguration configuration,
        ISingletonMutex singletonMutex) : base(configuration,logger)
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
                return await HttpResponseDataFactory.CreateForSuccess(req, CreateForServiceDiscovery(req));
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

    private GetHealthCheckResponse CreateForServiceDiscovery(HttpRequestData httpRequestData)
    {
        var baseUrl =
            $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";

        return new GetHealthCheckResponse()
        {
            Services = [
                new HealthCheckService()
                {
                    Name = "azure-cosmos-db",
                    FriendlyName = "Azure Cosmos DB",
                    ImageUrl = "https://img.icons8.com/color/48/000000/azure-cosmos-db.png",
                    Url = $"{baseUrl}/healthcheck/azure-cosmos-db"
                },
                new HealthCheckService()
                {
                    Name = "azure-email-communication-services",
                    FriendlyName = "Azure Email Communication Services",
                    ImageUrl = "https://img.icons8.com/color/48/000000/azure-cosmos-db.png",
                    Url = $"{baseUrl}/healthcheck/azure-email-communication-services"
                },
                new HealthCheckService()
                {
                    Name = "azure-storage",
                    FriendlyName = "Azure Storage",
                    ImageUrl = "https://img.icons8.com/color/48/000000/azure-cosmos-db.png",
                    Url = $"{baseUrl}/healthcheck/azure-storage"
                }
            ],
            TimeStamp = DateTime.UtcNow
        };
    }

    private async Task<HttpResponseData> PerformSpecificHealthCheck(HttpRequestData httpRequestData, string name)
    {
        // return the health check item for the specified name

        IHealthCheck? healthCheck = GetHealthCheckByName(name);
        if (healthCheck != null)
        {
            var healthCheckResult = await healthCheck.CheckHealthAsync();
            
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new GetHealthCheckServiceResponse()
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

    [ExcludeFromCodeCoverage(Justification = "Test health checks are not part of the production co")]
    private IHealthCheck? GetHealthCheckByName(string name)
    {
        switch (name)
        {
            // TODO: More health checks here, using name in GetHealthCheckResponse
            case "azure-cosmos-db": return new CosmosHealthCheck(_loggerFactory);
            case "test": return new TestHealthCheck();
            default: return null;
        }
    }
}