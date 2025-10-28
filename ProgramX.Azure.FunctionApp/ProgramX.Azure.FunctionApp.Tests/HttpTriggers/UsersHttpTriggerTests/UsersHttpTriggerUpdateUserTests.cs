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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
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
            roles = new List<Role>()
            {
                expectedRole
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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
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
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);;

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
    
    
    
    
    
    
    //
    // #region Helper Methods
    //
    // private TestHttpRequestData CreateMockHttpRequestWithAuth(string? userId = null)
    // {
    //     var mockFunctionContext = new Mock<FunctionContext>();
    //     var query = new NameValueCollection();
    //     
    //     var url = userId != null 
    //         ? new Uri($"https://localhost:7071/api/user/{userId}")
    //         : new Uri("https://localhost:7071/api/user");
    //         
    //     var testRequest = new TestHttpRequestData(mockFunctionContext.Object, query, url);
    //     
    //     // Add auth header - this would normally be handled by your auth setup
    //     testRequest.Headers.Add("Authorization", "Bearer valid-jwt-token");
    //     
    //     return testRequest;
    // }
    //
    // private TestHttpRequestData CreateMockHttpRequestWithoutAuth()
    // {
    //     var mockFunctionContext = new Mock<FunctionContext>();
    //     var query = new NameValueCollection();
    //     var url = new Uri("https://localhost:7071/api/user");
    //     
    //     return new TestHttpRequestData(mockFunctionContext.Object, query, url);
    //     // No Authorization header
    // }
    //
    // private void SetupGetSingleItemMock(string userId, PagedCosmosDbResult<User> result)
    // {
    //     // You'll need to setup your mocks to return this result
    //     // This depends on how your UsersHttpTriggerBuilder configures the mocks
    //     // For now, this is a placeholder - you'd need to setup the actual mock calls
    //     // that GetSingleItemAsync makes internally
    // }
    //
    // private void SetupGetPagedMultipleItemsMock(PagedCosmosDbResult<SecureUser> result)
    // {
    //     // Similar to above - setup mocks for GetPagedMultipleItemsAsync
    //     // This depends on your specific mock configuration in UsersHttpTriggerBuilder
    // }
    //
    private async Task<string> GetResponseBodyAsync(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
    }
    //
    // #endregion
    //
    // // Keep existing tests for CalculatePageUrls...
    // [Test]
    // public void CalculatePageUrls_WithBasicPaging_ShouldGenerateCorrectUrls()
    // {
    //     // Arrange
    //     var pagedResult = new PagedCosmosDbResult<SecureUser>(
    //         new List<SecureUser>(), 
    //         null, 
    //         10, 
    //         5.0, 
    //         5,
    //         0); // 5 total pages
    //
    //     pagedResult.SetTotalItems(50); // Mock total items
    //
    //     var baseUrl = "https://api.example.com/users";
    //
    //     // Act
    //     var result = InvokeCalculatePageUrls(pagedResult, baseUrl, null, null, null, null, 0, 
    //         10).ToArray();
    //
    //     // Assert
    //     result.Should().HaveCount(5);
    //     result.First().PageNumber.Should().Be(1);
    //     result.First().IsCurrentPage.Should().BeTrue();
    //     result.Last().PageNumber.Should().Be(5);
    //     Assert.IsTrue(result.All(p => p.Url.StartsWith(baseUrl)));
    // }
    //
    // [Test]
    // public void CalculatePageUrls_WithFilters_ShouldIncludeFiltersInUrls()
    // {
    //     // Arrange
    //     var pagedResult = new PagedCosmosDbResult<SecureUser>(
    //         new List<SecureUser>(), 
    //         null, 
    //         10, 
    //         3.0, 
    //         2,
    //         0);
    //
    //     pagedResult.SetTotalItems(20);
    //
    //     var baseUrl = "https://api.example.com/users";
    //     var containsText = "test";
    //     var withRoles = new[] { "Admin" };
    //
    //     // Act
    //     var result = InvokeCalculatePageUrls(pagedResult, baseUrl, containsText, withRoles, null, null, 0, 10).ToArray();
    //
    //     // Assert
    //     result.Should().HaveCount(2);
    //     result.All(p => p.Url.Contains("containsText="));
    //     result.All(p => p.Url.Contains("withRoles="));
    // }
    //
    // [TestCase(0, 10, 1)]
    // [TestCase(10, 10, 2)]
    // [TestCase(20, 10, 3)]
    // public void CalculatePageUrls_WithDifferentOffsets_ShouldCalculateCorrectCurrentPage(
    //     int offset, int itemsPerPage, int expectedCurrentPage)
    // {
    //     // Arrange
    //     var pagedResult = new PagedCosmosDbResult<SecureUser>(
    //         new List<SecureUser>(), 
    //         null, 
    //         itemsPerPage, 
    //         2.0, 
    //         5,
    //         0);
    //
    //     pagedResult.SetTotalItems(50);
    //
    //     var baseUrl = "https://api.example.com/users";
    //
    //     // Act
    //     var result = InvokeCalculatePageUrls(pagedResult, baseUrl, null, null, null, null, offset, itemsPerPage);
    //
    //     // Assert
    //     var currentPage = result.FirstOrDefault(p => p.IsCurrentPage);
    //     currentPage.Should().NotBeNull();
    //     currentPage!.PageNumber.Should().Be(expectedCurrentPage);
    // }
    //
    // private IEnumerable<UrlAccessiblePage> InvokeCalculatePageUrls(
    //     PagedCosmosDbResult<SecureUser> pagedResult, string baseUrl, string? containsText,
    //     IEnumerable<string>? withRoles, IEnumerable<string>? hasAccessToApplications, 
    //     string? continuationToken, int offset, int itemsPerPage)
    // {
    //     var method = typeof(UsersHttpTrigger).GetMethod("CalculatePageUrls", 
    //         BindingFlags.NonPublic | BindingFlags.Instance);
    //     
    //     return (IEnumerable<UrlAccessiblePage>)method!.Invoke(_usersHttpTrigger, 
    //         new object?[] { pagedResult, baseUrl, containsText, withRoles, hasAccessToApplications, 
    //             continuationToken, offset, itemsPerPage })!;
    // }
    //
    //
    #region Helper Methods


    private ItemResponse<T> CreateMockItemResponse<T>(HttpStatusCode statusCode)
    {
        var mockResponse = new Mock<ItemResponse<T>>();
        mockResponse.Setup(x => x.StatusCode).Returns(statusCode);
        return mockResponse.Object;
    }

    #endregion
}