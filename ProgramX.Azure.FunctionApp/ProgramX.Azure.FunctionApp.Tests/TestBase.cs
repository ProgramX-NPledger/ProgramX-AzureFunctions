using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Moq;
using NUnit.Framework;
using ProgramX.Azure.FunctionApp.Constants;

namespace ProgramX.Azure.FunctionApp.Tests;

public abstract class TestBase
{
    protected Mock<ILogger> MockLogger { get; private set; } = null!;
    protected Mock<CosmosClient> MockCosmosClient { get; private set; } = null!;
    protected Mock<Container> MockContainer { get; private set; } = null!;
    protected IConfiguration Configuration { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        MockLogger = new Mock<ILogger>();
        MockCosmosClient = new Mock<CosmosClient>();
        MockContainer = new Mock<Container>();
        
        MockCosmosClient
            .Setup(x => x.GetContainer(DataConstants.CoreDatabaseName, DataConstants.UsersContainerName))
            .Returns(MockContainer.Object);

        var mockDatabaseResponse = new Mock<DatabaseResponse>();
        mockDatabaseResponse
            .Setup(x=>x.StatusCode)
            .Returns(System.Net.HttpStatusCode.Created);
        
        MockCosmosClient
            .Setup(x => x.CreateDatabaseIfNotExistsAsync(
                It.IsAny<string>(),
                It.IsAny<ThroughputProperties>(),
                It.IsAny<RequestOptions>(),
                It.IsNotNull<CancellationToken>()))
            .ReturnsAsync(mockDatabaseResponse.Object);

        var mockContainerResponse = new Mock<ContainerResponse>();
        
        var mockDatabase = new Mock<Database>();
            
        mockDatabaseResponse
            .Setup(x=>x.Database)
            .Returns(mockDatabase.Object);

        mockDatabase.Setup(x => x.CreateContainerIfNotExistsAsync(It.IsAny<ContainerProperties>(), It.IsAny<int?>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContainerResponse.Object);

        
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();
    }

    [TearDown]
    public virtual void TearDown()
    {
        // Cleanup code if needed
    }
}
