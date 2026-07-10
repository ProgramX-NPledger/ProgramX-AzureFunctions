using System.Net;
using FluentAssertions;
using Moq;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("UpdateUserSettings")]
[TestFixture]
public class UsersHttpTriggerUpdateUserSettingsTests
{
    [Test]
    public async Task UpdateUserSettings_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        const string userName = "testuser";
        var updateSettingsRequest = new UpdateUserSettingsRequest { Theme = "dark" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(updateSettingsRequest)
            .Returns(HttpStatusCode.OK)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.UpdateUserSettingsAsync(It.IsAny<string>(), It.IsAny<string?>()))
                    .ReturnsAsync(new User { Id = "1", UserName = "testuser", EmailAddress = "test@example.com", Roles = [] });
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.UpdateUserSettings(testableHttpRequestData, userName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task UpdateUserSettings_WithNullBody_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithBody("null")
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.UpdateUserSettings(testableHttpRequestData, "testuser");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateUserSettings_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var updateSettingsRequest = new UpdateUserSettingsRequest { Theme = "dark" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(updateSettingsRequest)
            .Returns(HttpStatusCode.NotFound)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.UpdateUserSettingsAsync(It.IsAny<string>(), It.IsAny<string?>()))
                    .ThrowsAsync(new ItemNotFoundException());
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.UpdateUserSettings(testableHttpRequestData, "non-existent");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateUserSettings_WhenUpdateFails_ShouldReturnBadRequest()
    {
        // Arrange
        var updateSettingsRequest = new UpdateUserSettingsRequest { Theme = "dark" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(updateSettingsRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.UpdateUserSettingsAsync(It.IsAny<string>(), It.IsAny<string?>()))
                    .ThrowsAsync(new ItemUpdateException());
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.UpdateUserSettings(testableHttpRequestData, "testuser");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateUserSettings_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        var updateSettingsRequest = new UpdateUserSettingsRequest { Theme = "dark" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithPayload(updateSettingsRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.UpdateUserSettings(testableHttpRequestData, "testuser");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateUserSettings_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateSettingsRequest = new UpdateUserSettingsRequest { Theme = "dark" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithInvalidAuthentication()
            .WithPayload(updateSettingsRequest)
            .Returns(HttpStatusCode.Unauthorized)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.UpdateUserSettings(testableHttpRequestData, "testuser");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
