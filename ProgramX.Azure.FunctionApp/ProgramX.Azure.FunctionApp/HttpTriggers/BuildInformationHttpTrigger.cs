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

public class BuildInformationHttpTrigger(
    ILogger<HealthCheckHttpTrigger> logger,
    IConfiguration configuration)
    : AuthorisedHttpTriggerBase(configuration, logger)
{
    
    [Function(nameof(GetBuildInformation))]
    public async Task<HttpResponseData> GetBuildInformation(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "build")] HttpRequestData req, string? name)
    {
        return await HttpResponseDataFactory.CreateForSuccess(req, new GetBuildInformationResponse()
        {
            BuildNumber = Configuration["Commit:BuildNumber"] ?? "unknown",
            DeployedAt = Configuration["Commit:DeployedAt"] ?? "unknown",
            GitCommitHash = Configuration["Commit:CommitHash"] ?? "unknown",
        });
    }

    
}