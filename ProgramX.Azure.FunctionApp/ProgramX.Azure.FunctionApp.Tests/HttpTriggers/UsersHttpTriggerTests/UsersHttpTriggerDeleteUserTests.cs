using System.Collections.Specialized;
using System.Net;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Tests.Mocks;
using ProgramX.Azure.FunctionApp.Tests.TestData;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("DeleteUser")]
[TestFixture]
public class UsersHttpTriggerDeleteUserTests : TestBase
{

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
    }
   
    [Test]
    public async Task DeleteUser_WhenUserExists_ShouldDeleteSuccessfully()
    {
        // Arrange
        const string userId = "test-user-id";

        var existingUser = new User
        {
            id = userId,
            userName = "testuser",
            emailAddress = "test@example.com",
            passwordHash = new byte[]
            {
            },
            passwordSalt = new byte[]
            {
            }
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<User>(new List<User> { existingUser });
        
        // configure the container to return the user when queried by id
        mockedCosmosDbClientFactory.ConfigureContainerFunc = (mockContainer) =>
        {
            mockContainer.Setup(x => x.DeleteItemAsync<User>(
                    existingUser.id,
                    new PartitionKey(userId),
                    null,
                    CancellationToken.None))
                .ReturnsAsync(CreateMockItemResponse<User>(HttpStatusCode.NoContent));
        };
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        mockedCosmosDbClient.MockedContainer.Verify(x => x.DeleteItemAsync<User>(existingUser.id, new PartitionKey(userId), null, default), Times.Once);
    }

    [Test]
    public async Task DeleteUser_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NotFound)
            .Build();
        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<User>(new List<User>())
            {
                // configure the container to return the user when queried by id
                ConfigureContainerFunc = (mockContainer) =>
                {
                    mockContainer.Setup(x => x.DeleteItemAsync<User>(
                            It.IsAny<string>(),
                            new PartitionKey(It.IsAny<string>()),
                            null,
                            CancellationToken.None))
                        .ReturnsAsync(CreateMockItemResponse<User>(HttpStatusCode.NotFound));
                }
            };

        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(testableHttpRequestData, "does not exist");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        mockedCosmosDbClient.MockedContainer.Verify(x => x.DeleteItemAsync<User>(It.IsAny<string>(), new PartitionKey(It.IsAny<string>()), null, CancellationToken.None), Times.Never);
    }

    [Test]
    public async Task DeleteUser_WhenDeleteOperationFails_ShouldReturnServerError()
    {
        // Arrange
        const string userId = "test-user-id";

        var existingUser = new User
        {
            id = userId,
            userName = "testuser",
            emailAddress = "test@example.com",
            passwordHash = new byte[]
            {
            },
            passwordSalt = new byte[]
            {
            }
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.InternalServerError)
            .Build();
        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<User>(new List<User> { existingUser });
        
        // configure the container to return the user when queried by id
        mockedCosmosDbClientFactory.ConfigureContainerFunc = (mockContainer) =>
        {
            mockContainer.Setup(x => x.DeleteItemAsync<User>(
                    existingUser.id,
                    new PartitionKey(userId),
                    null,
                    CancellationToken.None))
                .ReturnsAsync(CreateMockItemResponse<User>(HttpStatusCode.InternalServerError));
        };
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        
        mockedCosmosDbClient.MockedContainer.Verify(x => x.DeleteItemAsync<User>(existingUser.id, new PartitionKey(userId), null, CancellationToken.None), Times.Once);

    }

    [Test]
    public async Task DeleteUser_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string userId = "test-user-id";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .Returns(HttpStatusCode.Unauthorized)
            .Build();        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<User>(new List<User>());
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        // no header is added so it is a bad request
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        mockedCosmosDbClient.MockedContainer.Verify(x => x.DeleteItemAsync<User>(It.IsAny<string>(), new PartitionKey(It.IsAny<string>()), null, CancellationToken.None), Times.Never);
    }

    [Test]
    public async Task DeleteUser_WhenCosmosThrowsException_ShouldReturnServerError()
    {
        // Arrange
        const string userId = "test-user-id";

        var existingUser = new User
        {
            id = userId,
            userName = "testuser",
            emailAddress = "test@example.com",
            passwordHash = new byte[]
            {
            },
            passwordSalt = new byte[]
            {
            }
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.InternalServerError)
            .Build();
        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<User>(new List<User> { existingUser });
        
        // configure the container to return the user when queried by id
        mockedCosmosDbClientFactory.ConfigureContainerFunc = (mockContainer) =>
        {
            mockContainer.Setup(x => x.DeleteItemAsync<User>(
                    existingUser.id,
                    new PartitionKey(userId),
                    null,
                    CancellationToken.None))
                .ThrowsAsync(new CosmosException("Database error", HttpStatusCode.InternalServerError, 500, "activityId", 1.0));
        };
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }


    
    #region Helper Methods


    private ItemResponse<T> CreateMockItemResponse<T>(HttpStatusCode statusCode)
    {
        var mockResponse = new Mock<ItemResponse<T>>();
        mockResponse.Setup(x => x.StatusCode).Returns(statusCode);
        return mockResponse.Object;
    }

    #endregion

}
