using System.Net;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class ApplicationsHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<LoginHttpTrigger> _logger;
    
    [CosmosDBOutput("core","applications",Connection="CosmosDBConnection")]
    public IEnumerable<Application> Applications { get; set; } = new List<Application>();
    
    public ApplicationsHttpTrigger(ILogger<LoginHttpTrigger> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GetApplications))]
    public async Task<GetApplicationsHttpResponse> GetApplications(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "applications")] HttpRequestData httpRequestData,
        [CosmosDBInput("core","applications",Connection = "CosmosDBConnection", SqlQuery = "SELECT * FROM c order by c.name")] IEnumerable<Application> applications)
    {
        await AssertAuthorisationAsync(httpRequestData);
        
        // https://charliedigital.com/2020/05/24/azure-functions-with-jwt-authentication/
        
        return new GetApplicationsHttpResponse(httpRequestData,applications);

    }
}