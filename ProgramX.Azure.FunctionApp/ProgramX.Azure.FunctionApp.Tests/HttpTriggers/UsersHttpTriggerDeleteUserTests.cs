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
                    default))
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
        const string userId = "non-existent-user";
        //const string authenticatedUser = "admin";
        
        var mockHttpRequest = CreateMockHttpRequestWithAuth();
        
        // Act
        var result = await _usersHttpTrigger.DeleteUser(mockHttpRequest, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        MockContainer.Verify(x => x.DeleteItemAsync<User>(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, default), Times.Never);
    }

    [Test]
    public async Task DeleteUser_WhenDeleteOperationFails_ShouldReturnServerError()
    {
        // Arrange
        const string userId = "test-user-id";
        //const string authenticatedUser = "admin";
        
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

        var mockHttpRequest = CreateMockHttpRequestWithAuth();
        
        MockContainer
            .Setup(x => x.DeleteItemAsync<User>(existingUser.id, new PartitionKey(userId), null, default))
            .ReturnsAsync(CreateMockItemResponse<User>(HttpStatusCode.InternalServerError));

        // Act
        var result = await _usersHttpTrigger.DeleteUser(mockHttpRequest, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        
        MockContainer.Verify(x => x.DeleteItemAsync<User>(existingUser.id, new PartitionKey(userId), null, default), Times.Once);
    }

    [Test]
    public async Task DeleteUser_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string userId = "test-user-id";
        
        var mockHttpRequest = CreateMockHttpRequestWithoutAuth();

        // Act
        var result = await _usersHttpTrigger.DeleteUser(mockHttpRequest.Object, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        MockContainer.Verify(x => x.DeleteItemAsync<User>(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, default), Times.Never);
    }

    [Test]
    public async Task DeleteUser_WhenCosmosThrowsException_ShouldReturnServerError()
    {
        // Arrange
        const string userId = "test-user-id";
//        const string authenticatedUser = "admin";
        
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

        var mockHttpRequest = CreateMockHttpRequestWithAuth();
        
        MockContainer
            .Setup(x => x.DeleteItemAsync<User>(existingUser.id, new PartitionKey(userId), null, default))
            .ThrowsAsync(new CosmosException("Database error", HttpStatusCode.InternalServerError, 500, "activityId", 1.0));

        // Act
        var result = await _usersHttpTrigger.DeleteUser(mockHttpRequest, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        
        MockContainer.Verify(x => x.DeleteItemAsync<User>(existingUser.id, new PartitionKey(userId), null, default), Times.Once);
    }


    
    #region Helper Methods

    private HttpRequestData CreateMockHttpRequestWithAuth(HttpStatusCode respondWithHttpStatusCode = HttpStatusCode.OK)
    {
        
        //var mockHttpRequestData = new Mock<HttpRequestData>();
        var mockFunctionContext = new Mock<FunctionContext>();
        var serviceCollection = new Mock<IServiceProvider>();
        
        var testHttpRequestData = new TestHttpRequestData(
            mockFunctionContext.Object, 
            _mockQuery, 
            new Uri("https://localhost:7071/api/user"),
            respondWithHttpStatusCode);

        return testHttpRequestData;
        
        // mockFunctionContext.Setup(x => x.InstanceServices).Returns(serviceCollection.Object);
        //
        // var stream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
        // var headers = new HttpHeadersCollection();
        // headers.Add("Authorization", $"Bearer valid-jwt-token");
        // var query = new NameValueCollection();
        //
        // return new Mock<HttpRequestData>(mockFunctionContext.Object, new Uri("https://localhost/api/v1/user"), "GET",
        //     headers, query, stream);

        
    }

    private Mock<HttpRequestData> CreateMockHttpRequestWithoutAuth()
    {
        var mockRequest = new Mock<HttpRequestData>();
        var mockFunctionContext = new Mock<FunctionContext>();
        
        mockRequest.Setup(x => x.FunctionContext).Returns(mockFunctionContext.Object);
        
        // Setup empty headers (no authorization)
        var headers = new HttpHeadersCollection();
        mockRequest.Setup(x => x.Headers).Returns(headers);
        
        return mockRequest;
    }

    private ItemResponse<T> CreateMockItemResponse<T>(HttpStatusCode statusCode)
    {
        var mockResponse = new Mock<ItemResponse<T>>();
        mockResponse.Setup(x => x.StatusCode).Returns(statusCode);
        return mockResponse.Object;
    }

    #endregion

}
