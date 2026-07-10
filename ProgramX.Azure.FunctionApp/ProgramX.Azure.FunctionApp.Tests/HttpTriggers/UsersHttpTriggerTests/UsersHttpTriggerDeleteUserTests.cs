using System.Net;
using FluentAssertions;
using Moq;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("DeleteUser")]
[TestFixture]
public class UsersHttpTriggerDeleteUserTests
{
    [Test]
    public async Task DeleteUser_WhenUserExists_ShouldReturnNoContent()
    {
        // Arrange
        const string userName = "testuser";

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.GetUserByUserNameAsync(It.IsAny<string>()))
                    .ReturnsAsync(new User { Id = "1", UserName = userName, EmailAddress = "test@example.com", Roles = [] });
                mockUserRepository
                    .Setup(x => x.DeleteUserByIdAsync(It.IsAny<string>()))
                    .Returns(Task.CompletedTask);
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(testableHttpRequestData, userName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteUser_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NotFound)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.GetUserByUserNameAsync(It.IsAny<string>()))
                    .ReturnsAsync((User?)null);
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(testableHttpRequestData, "does-not-exist");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteUser_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(testableHttpRequestData, "testuser");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task DeleteUser_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithInvalidAuthentication()
            .Returns(HttpStatusCode.Unauthorized)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.DeleteUser(testableHttpRequestData, "testuser");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
