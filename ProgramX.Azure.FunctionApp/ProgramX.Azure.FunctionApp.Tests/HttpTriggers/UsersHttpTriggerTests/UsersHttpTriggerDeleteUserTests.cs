using System.Net;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Moq;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Tests.Mocks;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("DeleteUser")]
[TestFixture]
public class UsersHttpTriggerDeleteUserTests
{
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
            },
            roles = new List<Role>()
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingUser);
                mockUserRepository.Setup(x => x.DeleteUserByIdAsync(It.IsAny<string>()));
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
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
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
                .WithIUserRepository(mockUserRepository =>
                {
                    mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                        .ReturnsAsync((SecureUser)null!);
                })
                .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(testableHttpRequestData, "does not exist");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
    public async Task DeleteUser_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string userId = "test-user-id";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithInvalidAuthentication()
            .Returns(HttpStatusCode.Unauthorized)
            .Build();        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<User>(new List<User>());
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        // no header is added so it is a bad request
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        mockedCosmosDbClient.MockedContainer.Verify(x => x.DeleteItemAsync<User>(It.IsAny<string>(), new PartitionKey(It.IsAny<string>()), null, CancellationToken.None), Times.Never);
    }

}
