using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.HttpTriggers;

namespace ProgramX.Azure.FunctionApp.Tests.TestData;

public class UsersHttpTriggerBuilder
{
    private Mock<ILogger<UsersHttpTrigger>>? _mockLogger;
    private Mock<CosmosClient>? _mockCosmosClient;
    private Mock<BlobServiceClient>? _mockBlobServiceClient;
    private IConfiguration? _configuration;
    private IRolesProvider? _rolesProvider;
    private IEmailSender? _emailSender;

    public UsersHttpTriggerBuilder WithLogger(Mock<ILogger<UsersHttpTrigger>> mockLogger)
    {
        _mockLogger = mockLogger;
        return this;
    }

    public UsersHttpTriggerBuilder WithCosmosClient(Mock<CosmosClient> mockCosmosClient)
    {
        _mockCosmosClient = mockCosmosClient;
        return this;
    }

    public UsersHttpTriggerBuilder WithBlobServiceClient(Mock<BlobServiceClient> mockBlobServiceClient)
    {
        _mockBlobServiceClient = mockBlobServiceClient;
        return this;
    }

    public UsersHttpTriggerBuilder WithConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        return this;
    }
    
    public UsersHttpTriggerBuilder WithRolesProvider(IRolesProvider rolesProvider)
    {
        _rolesProvider = rolesProvider;
        return this;
    }
    
    public UsersHttpTriggerBuilder WithEmailSender(IEmailSender emailSender)
    {
        _emailSender = emailSender;
        return this;
    }
    

    public UsersHttpTriggerBuilder WithDefaultMocks()
    {
        _mockLogger = new Mock<ILogger<UsersHttpTrigger>>();
        _mockCosmosClient = new Mock<CosmosClient>();
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        
        var mockContainer = new Mock<Container>();
        _mockCosmosClient
            .Setup(x => x.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockContainer.Object);

        return this;
    }

    public UsersHttpTrigger Build()
    {
        if (_mockLogger == null || _mockCosmosClient == null || _mockBlobServiceClient == null || _configuration == null)
        {
            throw new InvalidOperationException("All dependencies must be set before building");
        }

        return new UsersHttpTrigger(
            _mockLogger.Object,
            _mockCosmosClient.Object,
            _mockBlobServiceClient.Object,
            _configuration,
            _rolesProvider!,
            _emailSender!);
    }
}
