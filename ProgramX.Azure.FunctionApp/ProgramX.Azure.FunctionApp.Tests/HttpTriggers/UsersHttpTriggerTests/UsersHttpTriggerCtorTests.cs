using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("Ctor")]
[TestFixture]
public class UsersHttpTriggerCtorTests : TestBase
{
    private UsersHttpTrigger _usersHttpTrigger = null!;
    private Mock<ILogger<UsersHttpTrigger>> _mockSpecificLogger = null!;
    private Mock<BlobServiceClient> _mockBlobServiceClient = null!;
    private Mock<HttpRequestData> _mockHttpRequestData = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockSpecificLogger = new Mock<ILogger<UsersHttpTrigger>>();
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        _mockHttpRequestData = new Mock<HttpRequestData>();
        
        SetupCosmosDbReaderMocks();

        _usersHttpTrigger = new UsersHttpTrigger(
            _mockSpecificLogger.Object,
            MockCosmosClient.Object,
            _mockBlobServiceClient.Object,
            Configuration);
    }
    
    
    private void SetupCosmosDbReaderMocks()
    {
        // Instead of mocking the complex method call, create a concrete implementation
        // or mock it more simply by avoiding complex parameter matching
        
        // Option 1: Mock the CosmosClient methods directly instead of PagedCosmosDbReader
        var mockContainer = new Mock<Container>();
        var mockResponse = new Mock<ItemResponse<User>>();

        mockContainer.Setup(x => x.ReadItemAsync<User>(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, default))
            .ReturnsAsync(mockResponse.Object);

        MockCosmosClient.Setup(x => x.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockContainer.Object);
        
        // Option 2: If you must mock PagedCosmosDbReader, create a simple mock setup
        // that doesn't use complex parameter matching
    }


    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange, Act & Assert
        _usersHttpTrigger.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UsersHttpTrigger(
            null!,
            MockCosmosClient.Object,
            _mockBlobServiceClient.Object,
            Configuration));
    }

    [Test]
    public void Constructor_WithNullCosmosClient_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UsersHttpTrigger(
            _mockSpecificLogger.Object,
            null!,
            _mockBlobServiceClient.Object,
            Configuration));
    }

    [Test]
    public void Constructor_WithNullBlobServiceClient_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UsersHttpTrigger(
            _mockSpecificLogger.Object,
            MockCosmosClient.Object,
            null!,
            Configuration));
    }

    [Test]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UsersHttpTrigger(
            _mockSpecificLogger.Object,
            MockCosmosClient.Object,
            _mockBlobServiceClient.Object,
            null!));
    }
}
