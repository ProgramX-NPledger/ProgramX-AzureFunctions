using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using ProgramX.Azure.FunctionApp.Tests.Mocks;
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
    private Mock<IStorageClient> _mockStorageClient = null!;
    private Mock<HttpRequestData> _mockHttpRequestData = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockSpecificLogger = new Mock<ILogger<UsersHttpTrigger>>();
        _mockStorageClient = new Mock<IStorageClient>();
        _mockHttpRequestData = new Mock<HttpRequestData>();
        
        SetupCosmosDbReaderMocks();

        UserRepositoryFactory userRepositoryFactory = new UserRepositoryFactory();
        var mockedUserRepository = UserRepositoryFactory.CreateUserRepository();
        
        _usersHttpTrigger = new UsersHttpTrigger(
            _mockSpecificLogger.Object,
            _mockStorageClient.Object,
            Configuration,
            null!,
            mockedUserRepository.Object
            );
    }
    
    
    private void SetupCosmosDbReaderMocks()
    {
        var mockContainer = new Mock<Container>();
        var mockResponse = new Mock<ItemResponse<User>>();

        mockContainer.Setup(x => x.ReadItemAsync<User>(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, default))
            .ReturnsAsync(mockResponse.Object);

        MockCosmosClient.Setup(x => x.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockContainer.Object);
    }


    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange, Act & Assert
        _usersHttpTrigger.Should().NotBeNull();
    }



   
}
