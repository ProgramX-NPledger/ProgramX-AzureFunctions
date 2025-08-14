using System.Net;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class ApplicationsHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<LoginHttpTrigger> _logger;
    
  
    
    public ApplicationsHttpTrigger(ILogger<LoginHttpTrigger> logger, IConfiguration configuration) : base(configuration)    
    {
        _logger = logger;
    }

    [Function(nameof(GetApplications))]
    public async Task<HttpResponseData> GetApplications(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "application")] HttpRequestData httpRequestData)
    {
        return await RequiresAuthentication(httpRequestData, null, async () =>
        {
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new Application[0]);
        });
        
        // https://charliedigital.com/2020/05/24/azure-functions-with-jwt-authentication/
        
        

    }
}