using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.AzureCommunications;
using ProgramX.Azure.FunctionApp.AzureStorage;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Cosmos;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

/// <summary>
/// Provides Health Checks for services.
/// </summary>
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
    
    /// <summary>
    /// Returns a list of services that are eligible for health checks or performs a Health Check for a specific service.
    /// </summary>
    /// <param name="req">The <see cref="HttpRequestData"/> for the Azure HTTP Trigger.</param>
    /// <param name="name">Optional. If not specified, returns an array of <see cref="HealthCheckService"/> items which are eligible for health checks.
    /// Or, provide the name to perform a health check for a specific service.</param>
    /// <returns>An array of <see cref="HealthCheckService"/> items or a <see cref="GetHealthCheckForServiceResponse"/>.</returns>
    /// <remarks>Throttles health checks to once every minute. If requests are made within this minute, a 429 response is returned.</remarks>
    /// <response code="200">Returns a list of services that are eligible for health checks or performs a Health Check for a specific service.</response>
    /// <response code="429">Too many requests.</response>
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

        IServiceHealthCheck? healthCheck = GetHealthCheckByName(name);
        if (healthCheck != null)
        {
            var healthCheckResult = await healthCheck.CheckHealthAsync();
            
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new GetHealthCheckForServiceResponse()
            {
                Name = name,
                IsHealthy = healthCheckResult.IsHealthy, 
                Message = healthCheckResult.Message,
                TimeStamp = DateTime.UtcNow,
                SubItems = healthCheckResult.Items ?? new List<ServiceHealthCheckItemResult>()
            });
            
        }
        else
        {
            return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, $"No health check found for {name}");
        }
        
        
    }

    private IServiceHealthCheck? GetHealthCheckByName(string name)
    {
        switch (name)
        {
            // TODO: More health checks here, using name in GetHealthCheckResponse
            case "azure-storage": return new AzureStorageServiceHealthCheck(_loggerFactory);
            case "azure-cosmos-db": return new CosmosServiceHealthCheck(_loggerFactory);
            case "azure-email-communication-services": return new AzureCommunicationsServiceHealthCheck(_loggerFactory);
            default: return null;
        }
    }
}