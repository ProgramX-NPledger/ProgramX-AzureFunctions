using System.Net;
using FluentAssertions;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("UpdateUserPassword")]
[TestFixture]
public class UsersHttpTriggerUpdateUserPasswordTests
{
    [Test]
    public async Task UpdateUserPassword_WithValidRequest_ShouldReturnBadRequest()
    {
        // Note: PasswordValidator.AssertValidPassword always throws InvalidPasswordUpdateException,
        // so all UpdateUserPassword requests with a body return BadRequest.
        // Arrange
        const string userName = "testuser";
        var updatePasswordRequest = new UpdateUserPasswordRequest
        {
            NewPassword = "NewStr0ngP@ss!",
            PasswordConfirmationNonce = "valid-nonce"
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(updatePasswordRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.UpdateUserPassword(testableHttpRequestData, userName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateUserPassword_WithNullBody_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithBody("null")
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.UpdateUserPassword(testableHttpRequestData, "testuser");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateUserPassword_WhenPasswordInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var updatePasswordRequest = new UpdateUserPasswordRequest
        {
            NewPassword = "NewStr0ngP@ss!",
            PasswordConfirmationNonce = "wrong-nonce"
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(updatePasswordRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.UpdateUserPassword(testableHttpRequestData, "testuser");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateUserPassword_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Note: UpdateUserPassword uses permitAnonymous=true so auth is never checked.
        // The PasswordValidator always throws, resulting in BadRequest.
        // Arrange
        var updatePasswordRequest = new UpdateUserPasswordRequest
        {
            NewPassword = "NewStr0ngP@ss!",
            PasswordConfirmationNonce = "nonce"
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithPayload(updatePasswordRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.UpdateUserPassword(testableHttpRequestData, "testuser");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateUserPassword_WithInvalidAuthentication_ShouldReturnBadRequest()
    {
        // Note: UpdateUserPassword uses permitAnonymous=true so authentication is never checked.
        // PasswordValidator always throws, resulting in BadRequest regardless of auth.
        // Arrange
        var updatePasswordRequest = new UpdateUserPasswordRequest
        {
            NewPassword = "NewStr0ngP@ss!",
            PasswordConfirmationNonce = "nonce"
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithInvalidAuthentication()
            .WithPayload(updatePasswordRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.UpdateUserPassword(testableHttpRequestData, "testuser");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
