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
public class UsersHttpTriggerRemoveUserPhotoTests : TestBase
{
    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

    }
    
    
    [Test]
    public async Task RemoveUserPhoto_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        const string userId = "test-user-123";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
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
        var result = await usersHttpTrigger.RemoveUserPhoto(testableHttpRequestData, userId);
        
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
    }
    
    
    [Test]
    public async Task RemoveUserPhoto_WithValidId_ShouldReturnSuccess()
    {
        // Arrange
        const string userId = "test-user-123";
        var existingUser = new User
        {
            id = userId,
            userName = userId,
            emailAddress = "old@emailAddress.com",
            roles = new List<Role>(),
            profilePhotographOriginal = "somefile.png",
            profilePhotographSmall = "somesmallfile.png",
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
        var result = await usersHttpTrigger.RemoveUserPhoto(testableHttpRequestData, userId);
        
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify the response contains user and applications
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().NotBeNull();
        responseBody.Should().Contain("true");
    }
    
    
    [Test]
    public async Task RemoveUserPhoto_WithoutAuthentication_ShouldReturnBadRequest()
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
        var result = await usersHttpTrigger.RemoveUserPhoto(testableHttpRequestData, userId);
        
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
    }
    
    
    
    [Test]
    public async Task RemoveUserPhoto_WithInvalidAuthentication_ShouldReturnUnauthorized()
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
        var result = await usersHttpTrigger.RemoveUserPhoto(testableHttpRequestData, userId);
        
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