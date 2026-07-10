using System.Collections.Specialized;
using System.Net;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("GetUser")]
[TestFixture]
public class UsersHttpTriggerGetUserTests
{
    [Test]
    public async Task GetUser_WithValidUserName_ShouldReturnUser()
    {
        // Arrange
        const string userName = "testuser";

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.OK)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.GetUserByUserNameAsync(It.IsAny<string>()))
                    .ReturnsAsync(new User
                    {
                        Id = "1",
                        UserName = userName,
                        EmailAddress = "test@example.com",
                        FirstName = "Test",
                        LastName = "User",
                        Roles = [userName]
                    });
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData, userName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain(userName);
    }

    [Test]
    public async Task GetUser_WithNonExistentUserName_ShouldReturnNotFound()
    {
        // Arrange
        const string userName = "non-existent";

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
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData, userName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetUser_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .Returns(HttpStatusCode.OK)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData, "some-user");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetUser_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithInvalidAuthentication()
            .Returns(HttpStatusCode.OK)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData, "some-user");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetUser_WithoutId_ShouldReturnPagedUsers()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockResult = new Mock<IPagedResult<User>>();
                mockResult.SetupGet(x => x.Items).Returns([
                    new User { Id = "1", UserName = "user1", EmailAddress = "user1@example.com", Roles = [] },
                    new User { Id = "2", UserName = "user2", EmailAddress = "user2@example.com", Roles = [] }
                ]);
                mockResult.SetupGet(x => x.NumberOfPages).Returns(1);
                mockUserRepository
                    .Setup(x => x.GetUsersAsync(It.IsAny<GetUsersCriteria>(), It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData, null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("user1");
        body.Should().Contain("user2");
    }

    [Test]
    public async Task GetUser_WithContainsTextFilter_ShouldPassFilterToCriteria()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithQuery(new NameValueCollection { { "containsText", "john" } })
            .Build();

        GetUsersCriteria? capturedCriteria = null;
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockResult = new Mock<IPagedResult<User>>();
                mockResult.SetupGet(x => x.Items).Returns([
                    new User { Id = "1", UserName = "john", EmailAddress = "john@example.com", Roles = [] }
                ]);
                mockResult.SetupGet(x => x.NumberOfPages).Returns(1);
                mockUserRepository
                    .Setup(x => x.GetUsersAsync(It.IsAny<GetUsersCriteria>(), It.IsAny<PagedCriteria>()))
                    .Callback<GetUsersCriteria, PagedCriteria?>((c, _) => capturedCriteria = c)
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData, null);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedCriteria.Should().NotBeNull();
        capturedCriteria!.ContainingText.Should().Be("john");
    }

    [Test]
    public async Task GetUser_WithWithRolesFilter_ShouldPassRolesToCriteria()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithQuery(new NameValueCollection { { "withRoles", "admin,user" } })
            .Build();

        GetUsersCriteria? capturedCriteria = null;
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockResult = new Mock<IPagedResult<User>>();
                mockResult.SetupGet(x => x.Items).Returns([]);
                mockResult.SetupGet(x => x.NumberOfPages).Returns(1);
                mockUserRepository
                    .Setup(x => x.GetUsersAsync(It.IsAny<GetUsersCriteria>(), It.IsAny<PagedCriteria>()))
                    .Callback<GetUsersCriteria, PagedCriteria?>((c, _) => capturedCriteria = c)
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData, null);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedCriteria.Should().NotBeNull();
        capturedCriteria!.WithRoles.Should().BeEquivalentTo(["admin", "user"]);
    }

    [Test]
    public async Task GetUser_WithoutAuthentication_AndNoId_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .Returns(HttpStatusCode.OK)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData, null);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
