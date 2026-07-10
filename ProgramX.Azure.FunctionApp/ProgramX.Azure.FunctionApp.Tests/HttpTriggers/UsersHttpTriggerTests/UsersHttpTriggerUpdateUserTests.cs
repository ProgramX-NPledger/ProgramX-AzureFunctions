using System.Net;
using FluentAssertions;
using Moq;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("UpdateUser")]
[TestFixture]
public class UsersHttpTriggerUpdateUserTests : TestBase
{
    [SetUp]
    public override void SetUp() => base.SetUp();

    [Test]
    public async Task UpdateUser_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        const string userName = "testuser";
        var updateUserRequest = new UpdateUserRequest
        {
            EmailAddress = "updated@example.com",
            FirstName = "Updated",
            LastName = "User",
            Roles = ["admin"]
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(updateUserRequest)
            .Returns(HttpStatusCode.OK)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.UpdateUserAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                        It.IsAny<string?>(), It.IsAny<IEnumerable<string>?>()))
                    .ReturnsAsync(new User { Id = "1", UserName = "testuser", EmailAddress = "updated@example.com", Roles = [] });
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task UpdateUser_WithNullBody_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithBody("null")
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, "testuser");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateUser_WithRoles_ShouldPassRolesToRepository()
    {
        // Arrange
        const string userName = "testuser";
        var updateUserRequest = new UpdateUserRequest
        {
            EmailAddress = "user@example.com",
            Roles = ["admin", "user"]
        };

        IEnumerable<string>? capturedRoles = null;
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(updateUserRequest)
            .Returns(HttpStatusCode.OK)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.UpdateUserAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                        It.IsAny<string?>(), It.IsAny<IEnumerable<string>?>()))
                    .Callback<string, string, string?, string?, IEnumerable<string>>(
                        (_, _, _, _, roles) => capturedRoles = roles)
                    .ReturnsAsync(new User { Id = "1", UserName = "testuser", EmailAddress = "user@example.com", Roles = [] });
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, userName);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedRoles.Should().BeEquivalentTo(["admin", "user"]);
    }

    [Test]
    public async Task UpdateUser_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        var updateUserRequest = new UpdateUserRequest { EmailAddress = "user@example.com" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithPayload(updateUserRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, "testuser");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateUser_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateUserRequest = new UpdateUserRequest { EmailAddress = "user@example.com" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithInvalidAuthentication()
            .WithPayload(updateUserRequest)
            .Returns(HttpStatusCode.Unauthorized)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.UpdateUser(testableHttpRequestData, "testuser");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
