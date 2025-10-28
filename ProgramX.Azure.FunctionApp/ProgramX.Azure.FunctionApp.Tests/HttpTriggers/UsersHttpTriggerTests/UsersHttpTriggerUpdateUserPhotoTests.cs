using System.Collections.Specialized;
using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Tests.Mocks;
using ProgramX.Azure.FunctionApp.Tests.TestData;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("UpdateUserPhoto")]
[TestFixture]
public class UsersHttpTriggerUpdateUserPhotoTests : TestBase
{
    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

    }
    
    [Test]
    public async Task UpdateUserPhoto_WithNoContentTypeHeader_ShouldReturnBadRequest()
    {
        // Arrange
        const string userId = "test-user-123";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.BadRequest)
            .Build();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUserPhoto(testableHttpRequestData, userId);
        
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // Verify the response contains user and applications
        var responseBody = await GetResponseBodyAsync(result);
        
        responseBody.Should().NotBeNull();
        responseBody.Should().Contain("Content-Type");
    }
    
    
    
    
    [Test]
    public async Task UpdateUserPhoto_WithInvalidContentType_ShouldReturnBadRequest()
    {
        // Arrange
        var userId = "test-user-123";
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithHeaders(new NameValueCollection()
            {
                { "Content-Type", "application/json" }
            })
            .Returns(HttpStatusCode.BadRequest)
            .Build();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUserPhoto(testableHttpRequestData, userId);
        
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // Verify the response contains user and applications
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().NotBeNull();
        responseBody.Should().Contain("Content-Type");
    }
    
    
    [Test]
    public async Task UpdateUserPhoto_WithNoBoundary_ShouldReturnBadRequest()
    {
        // Arrange
        const string userId = "test-user-123";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithHeaders(new NameValueCollection()
            {
                { "Content-Type", "multipart/form-data" }
            })
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User>());
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUserPhoto(testableHttpRequestData, userId);
        
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
    }
    
    
    [Test]
    public async Task UpdateUserPhoto_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        const string userId = "test-user-123";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithHeaders(new NameValueCollection()
            {
                { "Content-Type", "multipart/form-data; boundary=---" }
            })
            .Returns(HttpStatusCode.NotFound)
            .Build();

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User>());
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUserPhoto(testableHttpRequestData, userId);
        
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
    }
    
    [Test]
    public async Task UpdateUserPhoto_WithNoNoMultipartSection_ShouldReturnBadRequest()
    {
        // Arrange
        const string userId = "test-user-123";
        var existingUser = new User
        {
            id = userId,
            userName = userId,
            emailAddress = "old@emailAddress.com",
            roles = new List<Role>(),
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
            .WithHeaders(new NameValueCollection()
            {
                { "Content-Type", "multipart/form-data; boundary=---" }
            })
            .Returns(HttpStatusCode.NotFound)
            .Build();

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User> { existingUser });
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();

        var mockedBlobServiceClientFactory = new MockedBlobServiceClientFactory();
        
        var mockedBlockServiceClient = mockedBlobServiceClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithBlobServiceClient(mockedBlockServiceClient)
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUserPhoto(testableHttpRequestData, userId);
        
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
    }
    
    
    [Test]
    public async Task UpdateUserPhoto_WithValidMultipartSection_ShouldReturnSuccess()
    {
        // Arrange
        const string userId = "test-user-123";
        var existingUser = new User
        {
            id = userId,
            userName = userId,
            emailAddress = "old@emailAddress.com",
            roles = new List<Role>(),
            passwordHash = new byte[]
            {
            },
            passwordSalt = new byte[]
            {
            }
        };

        var smallPhoto = new byte[] { 71, 73, 70, 56, 55, 97,       
            32, 0, 32, 0,                 
            129, 0, 0,                    
            0, 0, 255,                    
            0, 0, 0, 0, 0, 0, 0, 0, 0,    
            44,                           
            0, 0, 0, 0, 32, 0, 32, 0,     
            64,                           
            8, 53, 0, 1, 8, 28, 72, 176, 160, 193, 131, 8, 19, 42, 92, 200,
            176, 161, 195, 135, 16, 35, 74, 156, 72, 177, 162, 197, 139, 24,
            51, 106, 220, 200, 177, 163, 199, 143, 32, 67, 138, 28, 73, 178,
            164, 201, 147, 40, 83, 170, 92, 201, 82, 100, 64, 0,
            59 };
        var boundary = "---Boundary";
        var bodyStringBuilder = new StringBuilder();
        bodyStringBuilder.Append("--" + boundary + "\r\n");
        bodyStringBuilder.Append("Content-Disposition: form-data; name=\"file\"; filename=\"test.gif\"\r\n");
        bodyStringBuilder.Append("Content-Type: image/gif\r\n\r\n");
        bodyStringBuilder.Append(Convert.ToBase64String(smallPhoto));
        bodyStringBuilder.Append("\r\n");
        bodyStringBuilder.Append("--" + boundary + "--\r\n");
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithHeaders(new NameValueCollection()
            {
                { "Content-Type", $"multipart/form-data; boundary={boundary}" }
            })
            .WithBody(bodyStringBuilder.ToString())
            .Returns(HttpStatusCode.OK)
            .Build();

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User> { existingUser })
            {
                ConfigureContainerFunc = (mockContainer) =>
                {
                    mockContainer.Setup(c => c.ReplaceItemAsync(It.IsAny<User>(), It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(CreateMockItemResponse<User>(HttpStatusCode.OK));
                }
            };
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();

        var mockedBlobServiceClientFactory = new MockedBlobServiceClientFactory();
        
        var mockedBlockServiceClient = mockedBlobServiceClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithBlobServiceClient(mockedBlockServiceClient)
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUserPhoto(testableHttpRequestData, userId);
        
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify the response contains user and applications
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().NotBeNull();
        responseBody.Should().Contain("photoUrl");
    }
    
    
    [Test]
    public async Task UpdateUserPhoto_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        const string userId = "test-user-123";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .Returns(HttpStatusCode.BadRequest)
            .Build();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUserPhoto(testableHttpRequestData, userId);
        
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
    }
    
    
    
    [Test]
    public async Task UpdateUserPhoto_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string userId = "test-user-123";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithInvalidAuthentication()
            .Returns(HttpStatusCode.Unauthorized)
            .Build();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUserPhoto(testableHttpRequestData, userId);
        
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    
    private async Task<string> GetResponseBodyAsync(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
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