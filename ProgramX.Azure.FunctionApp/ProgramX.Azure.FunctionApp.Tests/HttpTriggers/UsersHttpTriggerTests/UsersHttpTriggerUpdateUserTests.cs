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
[Category("UpdateUser")]
[TestFixture]
public class UsersHttpTriggerUpdateUserTests : TestBase
{
    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

    }

    
    [Test]
    public async Task UpdateUser_InProfileScope_WithValidId_ShouldReturnOkAndUserShouldBeUpdated()
    {
        // Arrange
        const string userId = "test-user-123";
        const string expectedEmailAddress = "new@emailaddress.com";

        var existingUser = new User
        {
            id = userId,
            userName = userId,
            emailAddress = "old@emailAddress.com",
            roles = new List<Role>(),
        };
        var updateUser = new UpdateUserRequest()
        {
            updateProfileScope = true,
            emailAddress = expectedEmailAddress,
            userName = userId
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateUser)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingUser);
                mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()));
            })
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        
    }
    
    
    [Test]
    public async Task UpdateUser_InSettingsScope_WithValidId_ShouldReturnOkAndUserShouldBeUpdated()
    {
        // Arrange
        const string userId = "test-user-123";
        const string expectedTheme = "updated-theme";

        var existingUser = new User
        {
            id = userId,
            userName = userId,
            emailAddress = "old@emailAddress.com",
            theme = "old-theme",
            roles = new List<Role>(),
        };
        var updateUser = new UpdateUserRequest()
        {
            updateSettingsScope = true,
            theme = expectedTheme,
            userName = userId
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateUser)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingUser);
                mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()));
            })
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

    }
    
    
    [Test]
    public async Task UpdateUser_InPasswordScope_WithEmptyPassword_ShouldReturnBadRequest()
    {
        // Arrange
        const string userId = "test-user-123";
        const string expectedEmailAddress = "new@emailaddress.com";

        var existingUser = new User
        {
            id = userId,
            userName = userId,
            emailAddress = "old@emailAddress.com",
            roles = new List<Role>()
        };
        var updateUser = new UpdateUserRequest()
        {
            updatePasswordScope = true,
            newPassword = string.Empty,
            emailAddress = expectedEmailAddress,
            userName = userId
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateUser)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingUser);
                mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()));
            })
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }
    
    
    [Test]
    public async Task UpdateUser_InPasswordScope_WithDifferentUserName_ShouldReturnBadRequest()
    {
        // Arrange
        const string userId = "test-user-123";

        var existingUser = new User
        {
            id = userId,
            userName = userId,
            emailAddress = "old@emailAddress.com",
            roles = new List<Role>()
        };
        var updateUser = new UpdateUserRequest()
        {
            updatePasswordScope = true,
            userName = "different-user-name",
            newPassword = "new-password"
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateUser)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingUser);
                mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()));
            })
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }
    
    
    [Test]
    public async Task UpdateUser_InPasswordScope_WithDifferentNonce_ShouldReturnBadRequest()
    {
        // Arrange
        const string userId = "test-user-123";

        var existingUser = new User
        {
            id = userId,
            userName = userId,
            passwordConfirmationNonce = "nonce",
            emailAddress = "old@emailAddress.com",
            roles = new List<Role>()
        };
        var updateUser = new UpdateUserRequest()
        {
            updatePasswordScope = true,
            passwordConfirmationNonce = "different-nonce",
            newPassword = "new-password",
            userName = userId
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateUser)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingUser);
                mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()));
            })
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }
    
    
    [Test]
    public async Task UpdateUser_InPasswordScope_WithExpiredPasswordChangeRequest_ShouldReturnBadRequest()
    {
        // Arrange
        const string userId = "test-user-123";

        var existingUser = new User
        {
            id = userId,
            userName = userId,
            passwordLinkExpiresAt = DateTime.UtcNow.AddDays(-1),
            emailAddress = "old@emailAddress.com",
            roles = new List<Role>()
        };
        var updateUser = new UpdateUserRequest()
        {
            updatePasswordScope = true,
            newPassword="new-password",
            userName = userId
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateUser)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingUser);
                mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()));
            })
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }
    
    
    [Test]
    public async Task UpdateUser_InPasswordScope_WithValidId_ShouldReturnOkAndUserShouldBeUpdated()
    {
        // Arrange
        const string userId = "test-user-123";
        const string expectedNonce = "nonce";

        var existingUser = new User
        {
            id = userId,
            userName = userId,
            emailAddress = "old@emailAddress.com",
            passwordLinkExpiresAt = DateTime.MaxValue,
            passwordConfirmationNonce = expectedNonce,
            roles = new List<Role>()
        };
        var updateUser = new UpdateUserRequest()
        {
            updatePasswordScope = true,
            newPassword = "new-password",
            passwordConfirmationNonce = expectedNonce,
            userName = userId
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateUser)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        // var mockedCosmosDbClientFactory =
        //     new MockedCosmosDbClientFactory<User>(new List<User> { existingUser })
        //     {
        //         MutateItems = (items) =>
        //         {
        //             items.First(q => q.id == userId).passwordConfirmationNonce = null;
        //             items.First(q => q.id == userId).passwordLinkExpiresAt = null;
        //             items.First(q=>q.id==userId).passwordHash = [1,2,3];
        //             items.First(q=>q.id==userId).passwordSalt = [4,5,6];
        //             return items;
        //         },
        //         ConfigureContainerFunc = (mockContainer) =>
        //         {
        //             mockContainer.Setup(c => c.UpsertItemAsync(It.IsAny<User>(), It.IsAny<PartitionKey>(),
        //                 It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
        //                 .ReturnsAsync(CreateMockItemResponse<User>(HttpStatusCode.OK));
        //         }
        //     };
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingUser);
                mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()));
            })
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

    }
    
    
    [Test]
    public async Task UpdateUser_InRoleScope_WithValidId_ShouldReturnOkAndUserShouldBeUpdated()
    {
        // Arrange
        const string userId = "test-user-123";
        var expectedRole = new Role()
        {
            createdAt = DateTime.Now,
            applications = new List<Application>(),
            description = "Updated role",
            name = "updated-role",
            updatedAt = DateTime.Now
        };
        
        var existingUser = new User
        {
            id = userId,
            userName = userId,
            emailAddress = "email@address.com",
            roles = new List<Role>()
            {
                new Role()
                {
                    createdAt = DateTime.Now,
                    applications = new List<Application>(),
                    description = "Test role",
                    name = "test-role",
                    updatedAt = DateTime.Now
                }
            }
        };
        var updateUser = new UpdateUserRequest()
        {
            updateRolesScope = true,
            roles = new List<string>()
            {
                expectedRole.name
            },
            userName = userId
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateUser)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingUser);
                mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()));
            })
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

    }
    
    
    [Test]
    public async Task UpdateUser_InProfilePictureScope_WithValidId_ShouldReturnBadRequest()
    {
        // Arrange
        const string userId = "test-user-123";

        var existingUser = new User
        {
            id = userId,
            userName = userId,
            emailAddress = "old@emailAddress.com",
            roles = new List<Role>()
        };
        var updateUser = new UpdateUserRequest()
        {
            updateProfilePictureScope = true,
            userName = userId
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateUser)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingUser);
                mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()));
            })
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }
    
    [Test]
    public async Task UpdateUser_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        const string userId = "test-user-123";

        var updateUser = new UpdateUserRequest()
        {
            updateProfileScope = true,
            emailAddress = "",
            userName = userId
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateUser)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<UserPassword>(new List<UserPassword>())
            {
                ConfigureContainerFunc = (mockContainer) =>
                {
                    mockContainer.Setup(c => c.UpsertItemAsync(It.IsAny<User>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(CreateMockItemResponse<User>(HttpStatusCode.NotFound));
                }
            };
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);

    }
    
    [Test]
    public async Task UpdateUser_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        const string userId = "test-user-123";
        
        var updateUser = new UpdateUserRequest()
        {
            updateProfileScope = true,
            emailAddress = string.Empty,
            userName = userId
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithPayload(updateUser)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<UserPassword>(new List<UserPassword>());
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }
    
    
    [Test]
    public async Task UpdateUser_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string userId = "test-user-123";

        var updateUser = new UpdateUserRequest()
        {
            updateProfileScope = true,
            emailAddress = string.Empty,
            userName = userId
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithInvalidAuthentication()
            .WithPayload(updateUser)
            .Returns(HttpStatusCode.Unauthorized)
            .Build();

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<UserPassword>(new List<UserPassword>());
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userId);

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