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
using Microsoft.Extensions.Options;
using ProgramX.Azure.FunctionApp;
using ProgramX.Azure.FunctionApp.AzureCommunications;
using ProgramX.Azure.FunctionApp.AzureStorage;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Core;
using ProgramX.Azure.FunctionApp.Cosmos;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Osm;
using ProgramX.Azure.FunctionApp.Scouting.Contract;

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
        return new CosmosClient(connectionString, new CosmosClientOptions()
        {
            SerializerOptions = new CosmosSerializationOptions()
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        });
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
    .AddSingleton<IRoleRepository, CosmosRoleRepository>(serviceProvider =>
    {
        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
        return new CosmosRoleRepository(cosmosClient, serviceProvider.GetRequiredService<ILogger<CosmosRoleRepository>>());;
    })
    .AddSingleton<IScoutingRepository, CosmosScoutingRepository>(serviceProvider =>
    {
        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
        return new CosmosScoutingRepository(cosmosClient, serviceProvider.GetRequiredService<ILogger<CosmosScoutingRepository>>());;
    })    
    .AddSingleton<ISingletonMutex,SingletonMutex>()
    .AddSingleton<IApplicationProvider, CachingApplicationProvider>()
    .AddTransient<MultiPartContentHandler, MultiPartContentHandler>()
    .AddTransient<IEmailSender, AzureCommunicationsServicesEmailSender>(serviceProvoder =>
    {
        var configuration = serviceProvoder.GetRequiredService<IConfiguration>();
        return new AzureCommunicationsServicesEmailSender(configuration);
    })
    .AddHttpClient<IOsmClient, OsmClient>((serviceProvider, client) =>
    {
        var osmOptions = serviceProvider.GetRequiredService<IOptions<OsmOptions>>().Value;
        client.BaseAddress = osmOptions.BaseAddressUri;
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    })
    .AddHttpMessageHandler<AuthTokenHandler>();

// OSM authenticates with the OAuth client_credentials grant. The token is held by a singleton
// provider rather than by AuthTokenHandler, so it survives handler-pool rotation and is shared
// by every concurrent OSM call instead of being re-acquired per handler instance.
builder.Services
    .AddSingleton<IValidateOptions<OsmOptions>, OsmOptionsValidator>();

// Note: deliberately NOT .ValidateOnStart(). OsmOptionsValidator still runs, but lazily on first
// resolution of IOptions<OsmOptions>, so bad OSM configuration fails the OSM endpoints only.
// Validating at startup would abort host startup and take every unrelated endpoint (login, users,
// files) down with it — and the same package is deployed to both slots with no gated promotion.
// Add .ValidateOnStart() here once the Azure app settings are confirmed to carry the Osm:* keys.
builder.Services
    .AddOptions<OsmOptions>()
    .Bind(builder.Configuration.GetSection(OsmOptions.SectionName));

builder.Services
    .AddSingleton<IOsmTokenProvider, OsmClientCredentialsTokenProvider>();

// Deliberately has no AuthTokenHandler: acquiring a token must not require a token.
builder.Services
    .AddHttpClient(OsmClientCredentialsTokenProvider.TokenHttpClientName);

// Logging configuration
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Azure.Functions.Worker", LogLevel.Information);

builder.Build().Run();