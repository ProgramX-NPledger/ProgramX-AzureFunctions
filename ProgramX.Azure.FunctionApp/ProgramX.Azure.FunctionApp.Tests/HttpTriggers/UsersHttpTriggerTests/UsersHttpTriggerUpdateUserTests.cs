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
            passwordHash = new byte[]
            {
            },
            passwordSalt = new byte[]
            {
            }
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

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User> { existingUser })
            {
                MutateItems = (items) =>
                {   
                    items.First(q=>q.id==userId).emailAddress = expectedEmailAddress;
                    return items;
                },
                ConfigureContainerFunc = (mockContainer) =>
                {
                    mockContainer.Setup(c => c.UpsertItemAsync(It.IsAny<SecureUser>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(CreateMockItemResponse<SecureUser>(HttpStatusCode.OK));
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
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the response contains user and applications
        var responseBody = await GetResponseBodyAsync(result);
        
        // get the user
        var updatedUser = await usersHttpTrigger.GetUser(testableHttpRequestData, userId);
        
        updatedUser.Should().NotBeNull();
        updatedUser.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var getUserResponseBody = await GetResponseBodyAsync(updatedUser);
        getUserResponseBody.Should().Contain(expectedEmailAddress);
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
            passwordHash = new byte[]
            {
            },
            passwordSalt = new byte[]
            {
            }
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

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User> { existingUser })
            {
                MutateItems = (items) =>
                {   
                    items.First(q=>q.id==userId).theme = expectedTheme;
                    return items;
                },
                ConfigureContainerFunc = (mockContainer) =>
                {
                    mockContainer.Setup(c => c.UpsertItemAsync(It.IsAny<SecureUser>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(CreateMockItemResponse<SecureUser>(HttpStatusCode.OK));
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
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the response contains user and applications
        var responseBody = await GetResponseBodyAsync(result);
        
        // get the user
        var updatedUser = await usersHttpTrigger.GetUser(testableHttpRequestData, userId);
        
        updatedUser.Should().NotBeNull();
        updatedUser.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var getUserResponseBody = await GetResponseBodyAsync(updatedUser);
        getUserResponseBody.Should().Contain(expectedTheme);
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
            roles = new List<Role>(),
            passwordHash = new byte[]
            {
            },
            passwordSalt = new byte[]
            {
            }
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

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User> { existingUser });
        
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
    public async Task UpdateUser_InPasswordScope_WithDifferentUserName_ShouldReturnBadRequest()
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

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User> { existingUser });
        
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
            roles = new List<Role>(),
            passwordHash = new byte[]
            {
            },
            passwordSalt = new byte[]
            {
            }
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

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User> { existingUser });
        
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
            roles = new List<Role>(),
            passwordHash = new byte[]
            {
            },
            passwordSalt = new byte[]
            {
            }
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

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User> { existingUser });
        
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
            roles = new List<Role>(),
            passwordHash = new byte[]
            {
            },
            passwordSalt = new byte[]
            {
            }
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

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User> { existingUser })
            {
                MutateItems = (items) =>
                {   
                    items.First(q=>q.id==userId).passwordHash = [1,2,3];
                    items.First(q=>q.id==userId).passwordSalt = [4,5,6];
                    return items;
                },
                ConfigureContainerFunc = (mockContainer) =>
                {
                    mockContainer.Setup(c => c.UpsertItemAsync(It.IsAny<SecureUser>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(CreateMockItemResponse<SecureUser>(HttpStatusCode.OK));
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
            },
            passwordHash = new byte[]
            {
            },
            passwordSalt = new byte[]
            {
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

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User> { existingUser })
            {
                MutateItems = (items) =>
                {   
                    items.First(q=>q.id==userId).roles = new List<Role>() { expectedRole };
                    return items;
                },
                ConfigureContainerFunc = (mockContainer) =>
                {
                    mockContainer.Setup(c => c.UpsertItemAsync(It.IsAny<SecureUser>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(CreateMockItemResponse<SecureUser>(HttpStatusCode.OK));
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
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the response contains user and applications
        var responseBody = await GetResponseBodyAsync(result);
        
        // get the user
        var updatedUser = await usersHttpTrigger.GetUser(testableHttpRequestData, userId);
        
        updatedUser.Should().NotBeNull();
        updatedUser.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var getUserResponseBody = await GetResponseBodyAsync(updatedUser);
        getUserResponseBody.Should().Contain(expectedRole.name);
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
            roles = new List<Role>(),
            passwordHash = new byte[]
            {
            },
            passwordSalt = new byte[]
            {
            }
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

        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User> { existingUser });
        
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
            new MockedCosmosDbClientFactory<User>(new List<User>())
            {
                ConfigureContainerFunc = (mockContainer) =>
                {
                    mockContainer.Setup(c => c.UpsertItemAsync(It.IsAny<SecureUser>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(CreateMockItemResponse<SecureUser>(HttpStatusCode.NotFound));
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
            new MockedCosmosDbClientFactory<User>(new List<User>());
        
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
            new MockedCosmosDbClientFactory<User>(new List<User>());
        
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