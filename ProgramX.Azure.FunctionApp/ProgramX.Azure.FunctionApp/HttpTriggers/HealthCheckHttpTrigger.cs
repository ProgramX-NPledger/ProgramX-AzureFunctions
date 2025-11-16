using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class HealthCheckHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<HealthCheckHttpTrigger> _logger;

    public HealthCheckHttpTrigger(ILogger<HealthCheckHttpTrigger> logger, IConfiguration configuration) : base(configuration)
    {
        _logger = logger;
        
    }
    
    [Function(nameof(GetHealthCheck))]
    public async Task<HttpResponseData> GetHealthCheck(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "healthcheck/{name?}")] HttpRequestData req, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return await HttpResponseDataFactory.CreateForSuccess(req, new GetHealthCheckResponse());
        }
        else
        {
            return await PerformSpecificHealthCheck(req, name);       
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
            Message = string.Empty
        });
        
    }

}