using System.ClientModel;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Responses;
using ProgramX.Azure.FunctionApp.Tests.TestData;
using System.Collections.Specialized;
using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using ProgramX.Azure.FunctionApp.Tests.Mocks;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("DeleteUser")]
[TestFixture]
public class UsersHttpTriggerDeleteUserTests : TestBase
{
    private UsersHttpTrigger _usersHttpTrigger = null!;
    private Mock<PagedCosmosDbReader<SecureUser>> _mockSecureUserReader = null!;
    private Mock<PagedCosmosDbReader<User>> _mockUserReader = null!;
    private Mock<HttpRequestData> _mockHttpRequestData = null!;
    private NameValueCollection _mockQuery = null!;
    private Mock<FunctionContext> _mockFunctionContext = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        
        _mockSecureUserReader = new Mock<PagedCosmosDbReader<SecureUser>>(
            MockCosmosClient.Object, 
            DataConstants.CoreDatabaseName, 
            DataConstants.UsersContainerName, 
            DataConstants.UserNamePartitionKeyPath);
        
        _mockUserReader = new Mock<PagedCosmosDbReader<User>>(
            MockCosmosClient.Object, 
            DataConstants.CoreDatabaseName, 
            DataConstants.UsersContainerName, 
            DataConstants.UserNamePartitionKeyPath);
        
        // _usersHttpTrigger = new UsersHttpTriggerBuilder()
        //     .WithDefaultMocks()
        //     .WithConfiguration(Configuration)
        //     .Build();
        
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

        var mockHttpRequest = CreateMockHttpRequestWithAuth(HttpStatusCode.NoContent);

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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(mockHttpRequest, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        mockedCosmosDbClient.MockedContainer.Verify(x => x.DeleteItemAsync<User>(existingUser.id, new PartitionKey(userId), null, default), Times.Once);
    }

    [Test]
    public async Task DeleteUser_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var mockHttpRequest = CreateMockHttpRequestWithAuth(HttpStatusCode.NotFound);

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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(mockHttpRequest, "does not exist");

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

        var mockHttpRequest = CreateMockHttpRequestWithAuth(HttpStatusCode.InternalServerError);

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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(mockHttpRequest, userId);

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
        
        var mockHttpRequest = CreateMockHttpRequestWithoutAuth();
        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<User>(new List<User>());
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(mockHttpRequest, userId);

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

        var mockHttpRequest = CreateMockHttpRequestWithAuth(HttpStatusCode.InternalServerError);

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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(mockHttpRequest, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }


    
    #region Helper Methods

    private HttpRequestData CreateMockHttpRequestWithAuth(HttpStatusCode respondWithHttpStatusCode = HttpStatusCode.OK)
    {
        var mockFunctionContext = new Mock<FunctionContext>();
        
        var testHttpRequestData = new TestHttpRequestData(
            mockFunctionContext.Object, 
            _mockQuery, 
            new Uri("https://localhost:7071/api/user"),
            respondWithHttpStatusCode);

        return testHttpRequestData;
        
    }

    private HttpRequestData CreateMockHttpRequestWithoutAuth()
    {
        var mockFunctionContext = new Mock<FunctionContext>();
        
        var testHttpRequestData = new TestHttpRequestData(
            mockFunctionContext.Object, 
            _mockQuery, 
            new Uri("https://localhost:7071/api/user"),
            HttpStatusCode.Unauthorized,
            [],
            false);

        return testHttpRequestData;
    }

    private ItemResponse<T> CreateMockItemResponse<T>(HttpStatusCode statusCode)
    {
        var mockResponse = new Mock<ItemResponse<T>>();
        mockResponse.Setup(x => x.StatusCode).Returns(statusCode);
        return mockResponse.Object;
    }

    #endregion

}
