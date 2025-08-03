using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class HealthCheckHttpTrigger
{
    private readonly ILogger<HealthCheckHttpTrigger> _logger;

    public HealthCheckHttpTrigger(ILogger<HealthCheckHttpTrigger> logger)
    {
        _logger = logger;
        
    }
    
    [Function(nameof(GetHealthCheck))]
    public IActionResult GetHealthCheck([HttpTrigger(AuthorizationLevel.Function, "get", Route = "healthcheck")] HttpRequestData req)
    {
        return new OkObjectResult("OK");
    }
    
    
}