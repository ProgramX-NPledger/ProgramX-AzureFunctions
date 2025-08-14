using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProgramX.Azure.FunctionApp.Helpers;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();




builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddSingleton<JwtTokenIssuer,JwtTokenIssuer>()
    .AddSingleton<CosmosClient,CosmosClient>(cosmosClient =>
    {
        string connectionString = Environment.GetEnvironmentVariable("CosmosDBConnection");
        
        return new CosmosClient(connectionString);
    });

builder.Build().Run();