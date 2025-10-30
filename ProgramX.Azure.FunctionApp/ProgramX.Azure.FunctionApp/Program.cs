using Azure.Core.Serialization;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Core;
using ProgramX.Azure.FunctionApp.Helpers;

var builder = FunctionsApplication.CreateBuilder(args);

DependencyInjectionConfiguration.ConfigureServices(builder.Services);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddSingleton<JwtTokenIssuer, JwtTokenIssuer>()
    .AddSingleton<CosmosClient, CosmosClient>(cosmosClient =>
    {
        string connectionString = Environment.GetEnvironmentVariable("CosmosDBConnection");
        return new CosmosClient(connectionString);
    })
    .AddSingleton<BlobServiceClient, BlobServiceClient>(blobService =>
    {
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        return new BlobServiceClient(connectionString);
    })
    .AddTransient<IRolesProvider, CosmosDBRolesProvider>(serviceProvider =>
    {
        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
        return new CosmosDBRolesProvider(cosmosClient);
    })
    .AddTransient<IEmailSender, AzureCommunicationsServicesEmailSender>(serviceProvoder =>
    {
        var configuration = serviceProvoder.GetRequiredService<IConfiguration>();
        return new AzureCommunicationsServicesEmailSender(configuration);
    });
        


builder.Build().Run();