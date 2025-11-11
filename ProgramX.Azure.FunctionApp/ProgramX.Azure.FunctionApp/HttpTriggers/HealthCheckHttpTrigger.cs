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
            return await HttpResponseDataFactory.CreateForSuccess(req, PerformBasicHealthCheck());
        }
        else
        {
            return await PerformSpecificHealthCheck(req, name);       
        }
    }

    private async Task<HttpResponseData> PerformSpecificHealthCheck(HttpRequestData httpRequestData, string name)
    {
        // return the health check item for the specified name
        
    }

    private GetHealthCheckResponse PerformBasicHealthCheck()
    {
        _logger.LogInformation("HealthCheck request received.");

        GetHealthCheckResponse getHealthCheckResponse;
        if (Authentication == null)
        {
            getHealthCheckResponse = new GetHealthCheckResponse();
        }
        else
        {
            getHealthCheckResponse = new GetAuthenticatedHealthCheckResponse();
            ((List<HealthCheckItem>)getHealthCheckResponse.HealthCheckItems).AddRange([
                new HealthCheckItem()
                {
                    Name = "azure-communication-services-email",
                    FriendlyName = "Azure Communication Services (Email)",
                    ImageUrl = "https://img.icons8.com/color/48/000000/azure-web-apps.png" // TODO: Azure Storage
                },
                new HealthCheckItem()
                {
                    Name = "azure-storage",
                    FriendlyName = "Azure Storage",
                    ImageUrl = "https://img.icons8.com/color/48/000000/azure-web-apps.png" // TODO: Azure Storage
                },
                new HealthCheckItem()
                {
                    Name = "azure-cosmos-db",
                    FriendlyName = "Azure Cosmos DB",
                    ImageUrl = "https://img.icons8.com/color/48/000000/azure-web-apps.png" // TODO: Azure Storage
                }
            ]);
            return getHealthCheckResponse;
        }
        
        return getHealthCheckResponse;
    }
}