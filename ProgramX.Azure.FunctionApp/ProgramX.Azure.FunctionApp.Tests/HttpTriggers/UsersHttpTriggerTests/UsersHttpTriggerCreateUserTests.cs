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
[Category("CreateUser")]
[TestFixture]
public class UsersHttpTriggerCreateUserTests
{
    [Test]
    public async Task CreateUser_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange
        const string userName = "newuser";
        var createUserRequest = new CreateUserRequest
        {
            UserName = userName,
            EmailAddress = "new@example.com",
            FirstName = "New",
            LastName = "User",
            AddToRoles = ["admin"]
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .GrantRoles(["admin"])
            .WithPayload(createUserRequest)
            .Returns(HttpStatusCode.Created)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.CreateUserAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                        It.IsAny<IEnumerable<string>?>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(new User
                    {
                        Id = "1",
                        UserName = userName,
                        EmailAddress = createUserRequest.EmailAddress,
                        Roles = [],
                        PasswordConfirmationNonce = "nonce",
                        PasswordLinkExpiresAt = DateTime.UtcNow.AddDays(1)
                    });
            })
            .WithEmailSender(mockEmailSender =>
            {
                mockEmailSender
                    .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>()))
                    .Returns(Task.CompletedTask);
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.CreateUser(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Test]
    public async Task CreateUser_WithNullBody_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .GrantRoles(["admin"])
            .WithBody("null")
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.CreateUser(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateUser_WhenUserAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var createUserRequest = new CreateUserRequest
        {
            UserName = "existing-user",
            EmailAddress = "existing@example.com",
            AddToRoles = []
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .GrantRoles(["admin"])
            .WithPayload(createUserRequest)
            .Returns(HttpStatusCode.Conflict)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.CreateUserAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                        It.IsAny<IEnumerable<string>?>(), It.IsAny<DateTime>()))
                    .ThrowsAsync(new ItemAlreadyExistsException());
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.CreateUser(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task CreateUser_WhenCreationFails_ShouldReturnBadRequest()
    {
        // Arrange
        var createUserRequest = new CreateUserRequest
        {
            UserName = "new-user",
            EmailAddress = "new@example.com",
            AddToRoles = []
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .GrantRoles(["admin"])
            .WithPayload(createUserRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.CreateUserAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                        It.IsAny<IEnumerable<string>?>(), It.IsAny<DateTime>()))
                    .ThrowsAsync(new ItemCreationException());
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.CreateUser(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateUser_WhenEmailSendFails_ShouldReturnServerError()
    {
        // Arrange
        var createUserRequest = new CreateUserRequest
        {
            UserName = "new-user",
            EmailAddress = "new@example.com",
            AddToRoles = []
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .GrantRoles(["admin"])
            .WithPayload(createUserRequest)
            .Returns(HttpStatusCode.InternalServerError)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.CreateUserAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                        It.IsAny<IEnumerable<string>?>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(new User
                    {
                        Id = "1",
                        UserName = "new-user",
                        EmailAddress = "new@example.com",
                        Roles = [],
                        PasswordConfirmationNonce = "nonce",
                        PasswordLinkExpiresAt = DateTime.UtcNow.AddDays(1)
                    });
            })
            .WithEmailSender(mockEmailSender =>
            {
                mockEmailSender
                    .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>()))
                    .ThrowsAsync(new InvalidOperationException("Email send failed"));
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.CreateUser(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task CreateUser_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        var createUserRequest = new CreateUserRequest
        {
            UserName = "new-user",
            EmailAddress = "new@example.com",
            AddToRoles = []
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithPayload(createUserRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

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
        var createUserRequest = new CreateUserRequest
        {
            UserName = "new-user",
            EmailAddress = "new@example.com",
            AddToRoles = []
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithInvalidAuthentication()
            .WithPayload(createUserRequest)
            .Returns(HttpStatusCode.Unauthorized)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.CreateUser(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
