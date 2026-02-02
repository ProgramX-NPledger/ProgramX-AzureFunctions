using System.Net.Http.Headers;
using Azure.Core.Serialization;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp;
using ProgramX.Azure.FunctionApp.AzureStorage;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Core;
using ProgramX.Azure.FunctionApp.Cosmos;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Osm;

var builder = FunctionsApplication.CreateBuilder(args);

DependencyInjectionConfiguration.ConfigureServices(builder.Services);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddTransient<AuthTokenHandler>()
    .AddSingleton<JwtTokenIssuer, JwtTokenIssuer>()
    .AddSingleton<CosmosClient, CosmosClient>(cosmosClient =>
    {
        string? connectionString = Environment.GetEnvironmentVariable("CosmosDBConnection");
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("CosmosDBConnection environment variable is not set");
        return new CosmosClient(connectionString);
    })
    .AddSingleton<BlobServiceClient, BlobServiceClient>(blobService =>
    {
        string? connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("AzureWebJobsStorage environment variable is not set");
        return new BlobServiceClient(connectionString);
    })
    .AddSingleton<IStorageClient, AzureStorageClient>(serviceProvider => new AzureStorageClient(serviceProvider.GetRequiredService<BlobServiceClient>()))
    .AddSingleton<IUserRepository, CosmosUserRepository>(serviceProvider =>
    {
        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
        return new CosmosUserRepository(cosmosClient, serviceProvider.GetRequiredService<ILogger<CosmosUserRepository>>());;
    })
    .AddSingleton<IScoutingRepository, CosmosScoutingRepository>(serviceProvider =>
    {
        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
        return new CosmosScoutingRepository(cosmosClient, serviceProvider.GetRequiredService<ILogger<CosmosScoutingRepository>>());;
    })    
    .AddSingleton<IIntegrationRepository, CosmosIntegrationRepository>(serviceProvider =>
    {
        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
        return new CosmosIntegrationRepository(cosmosClient, serviceProvider.GetRequiredService<ILogger<CosmosIntegrationRepository>>());;
    })
    .AddSingleton<ISingletonMutex,SingletonMutex>()
    .AddSingleton<IResetApplication,ResetApplication>()
    .AddTransient<IEmailSender, AzureCommunicationsServicesEmailSender>(serviceProvoder =>
    {
        var configuration = serviceProvoder.GetRequiredService<IConfiguration>();
        return new AzureCommunicationsServicesEmailSender(configuration);
    })
    .AddHttpClient<IOsmClient, OsmClient>(client =>
    {
        client.BaseAddress = new Uri("https://www.onlinescoutmanager.co.uk/");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    })
    .AddHttpMessageHandler<AuthTokenHandler>();
        


builder.Build().Run();