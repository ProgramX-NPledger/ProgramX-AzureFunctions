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
    public async Task<HttpResponseData> GetHealthCheck([HttpTrigger(AuthorizationLevel.Function, "get", Route = "healthcheck")] HttpRequestData req)
    {
        _logger.LogInformation("HealthCheck request received.");
        
        var httpResponseData = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await httpResponseData.WriteAsJsonAsync(new
        {
            status = "OK",
            azureFunctions = true,
            azureCosmosDb = (bool?)null,
            azureStorage = (bool?)null,
        });
        
        return httpResponseData;
        
    }
    
    
}