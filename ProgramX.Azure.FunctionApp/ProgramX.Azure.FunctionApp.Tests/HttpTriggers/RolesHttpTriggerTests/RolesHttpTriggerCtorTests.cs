using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.RolesHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("RolesHttpTrigger")]
[Category("Ctor")]
[TestFixture]
public class RolesHttpTriggerCtorTests : TestBase
{
    private RolesHttpTrigger _rolesHttpTrigger = null!;
    private Mock<ILogger<RolesHttpTrigger>> _mockSpecificLogger = null!;
    private Mock<IStorageClient> _mockStorageClient = null!;
    private Mock<HttpRequestData> _mockHttpRequestData = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockSpecificLogger = new Mock<ILogger<RolesHttpTrigger>>();
        _mockStorageClient = new Mock<IStorageClient>();
        _mockHttpRequestData = new Mock<HttpRequestData>();
        
        var mockedUserRepository = UserRepositoryFactory.CreateUserRepository();
        
        _rolesHttpTrigger = new RolesHttpTrigger(
            _mockSpecificLogger.Object,
            Configuration,
            mockedUserRepository.Object
            );
    }
    
    

    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange, Act & Assert
        _rolesHttpTrigger.Should().NotBeNull();
    }

}
