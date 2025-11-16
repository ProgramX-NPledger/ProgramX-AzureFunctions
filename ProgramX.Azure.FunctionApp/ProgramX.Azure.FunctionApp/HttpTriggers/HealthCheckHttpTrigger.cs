using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class HealthCheckHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<HealthCheckHttpTrigger> _logger;
    private readonly ISingletonMutex _singletonMutex;

    public HealthCheckHttpTrigger(ILogger<HealthCheckHttpTrigger> logger, 
        IConfiguration configuration,
        ISingletonMutex singletonMutex) : base(configuration)
    {
        _logger = logger;
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
        // TODO: Perform the health check
        return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new HealthCheckItemResponse()
        {
            Name = name,
            IsHealthy = false, 
            Message = string.Empty,
            TimeStamp = DateTime.UtcNow
        });
        
    }

}