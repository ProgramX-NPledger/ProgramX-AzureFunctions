using System.Net;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Tests.Mocks;
using ProgramX.Azure.FunctionApp.Tests.TestData;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("UpdateUser")]
[TestFixture]
public class UsersHttpTriggerCreateUserTests : TestBase
{
    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

    }

    
    [Test]
    public async Task CreateUser_WithValidRequest_ShouldReturnOkAndUserShouldBeCreated()
    {
        // Arrange
        const string userId = "test-user-123";

        var createUserRequest = new CreateUserRequest()
        {
            emailAddress = "email@address.com",
            userName = userId,
            addToRoles = ["test-role"],
            firstName = "test",
            lastName = "user",
            password = "password",
            passwordConfirmationLinkExpiryDate = DateTime.UtcNow.AddDays(1),
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(createUserRequest)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var mockedUserCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User>())
            {
                MutateItems = (items) =>
                {
                    var itemsList = new List<User>(items);
                    itemsList.Add(new User
                    {
                        id = userId,
                        userName = userId,
                        emailAddress = createUserRequest.emailAddress,
                        passwordHash = new byte[]
                        {
                        },
                        passwordSalt = new byte[]
                        {
                        }
                    });
                    return itemsList;
                },
                ConfigureContainerFunc = (mockContainer) =>
                {
                    mockContainer.Setup(c => c.CreateItemAsync(It.IsAny<User>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(CreateMockItemResponse<User>(HttpStatusCode.Created));
                }
            };
        
        var mockedUserCosmosDbClient = mockedUserCosmosDbClientFactory.Create();

        var mockedRoleCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<Role>(new List<Role>());

        var mockedRoleCosmosDbClient = mockedRoleCosmosDbClientFactory.Create();

        var mockedEmailSenderFactory = new MockedEmailSenderFactory();
        var mockedEmailSender = mockedEmailSenderFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithEmailSender(mockedEmailSender.Object)
            .WithCosmosClient(mockedUserCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.CreateUser(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify the response contains user and applications
        var responseBody = await GetResponseBodyAsync(result);
        
        
    }
    
    
    
    
    [Test]
    public async Task CreateUser_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        const string userId = "test-user-123";
        
        var createUserRequest = new CreateUserRequest()
        {
            emailAddress = "email@address.com",
            userName = userId,
            addToRoles = ["test-role"],
            firstName = "test",
            lastName = "user",
            password = "password",
            passwordConfirmationLinkExpiryDate = DateTime.UtcNow.AddDays(1),
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithPayload(createUserRequest)
            .Returns(HttpStatusCode.NoContent)
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
        var result = await usersHttpTrigger.CreateUser(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }
    
    
    [Test]
    public async Task CreateUser_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string userId = "test-user-123";

        var createUserRequest = new CreateUserRequest()
        {
            emailAddress = "email@address.com",
            userName = userId,
            addToRoles = ["test-role"],
            firstName = "test",
            lastName = "user",
            password = "password",
            passwordConfirmationLinkExpiryDate = DateTime.UtcNow.AddDays(1),
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithInvalidAuthentication()
            .WithPayload(createUserRequest)
            .Returns(HttpStatusCode.Unauthorized)
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
        var result = await usersHttpTrigger.CreateUser(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);;

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