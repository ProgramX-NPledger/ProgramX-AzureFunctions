using System.Net;
using FluentAssertions;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.RolesHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("RolesHttpTrigger")]
[Category("CreateRole")]
[TestFixture]
public class RolesHttpTriggerCreateRoleTests
{
    [Test]
    public async Task CreateRole_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange
        const string roleName = "test-role";
        var createRoleRequest = new CreateRoleRequest
        {
            Name = roleName,
            Description = "test role description",
            AddToUsers = []
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(createRoleRequest)
            .Returns(HttpStatusCode.Created)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                mockRoleRepository
                    .Setup(x => x.CreateRoleAsync(It.IsAny<string>(), It.IsAny<string?>()))
                    .ReturnsAsync(new Role { RoleName = roleName, Description = createRoleRequest.Description });
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.CreateRole(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Test]
    public async Task CreateRole_WithUsersToAdd_ShouldAddUsersAndReturnCreated()
    {
        // Arrange
        const string roleName = "test-role";
        var createRoleRequest = new CreateRoleRequest
        {
            Name = roleName,
            Description = "a role",
            AddToUsers = ["user1", "user2"]
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(createRoleRequest)
            .Returns(HttpStatusCode.Created)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                mockRoleRepository
                    .Setup(x => x.CreateRoleAsync(It.IsAny<string>(), It.IsAny<string?>()))
                    .ReturnsAsync(new Role { RoleName = roleName });
            })
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.AddRoleToUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new User { Id = "1", UserName = "user1", EmailAddress = "u@t.com", Roles = [roleName] });
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.CreateRole(testableHttpRequestData);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Test]
    public async Task CreateRole_WithNullBody_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithBody("null")
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder().Build();

        // Act
        var result = await rolesHttpTrigger.CreateRole(testableHttpRequestData);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateRole_WhenRoleAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var createRoleRequest = new CreateRoleRequest { Name = "existing-role" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(createRoleRequest)
            .Returns(HttpStatusCode.Conflict)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                mockRoleRepository
                    .Setup(x => x.CreateRoleAsync(It.IsAny<string>(), It.IsAny<string?>()))
                    .ThrowsAsync(new ItemAlreadyExistsException());
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.CreateRole(testableHttpRequestData);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task CreateRole_WhenCreationFails_ShouldReturnBadRequest()
    {
        // Arrange
        var createRoleRequest = new CreateRoleRequest { Name = "new-role" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(createRoleRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                mockRoleRepository
                    .Setup(x => x.CreateRoleAsync(It.IsAny<string>(), It.IsAny<string?>()))
                    .ThrowsAsync(new ItemCreationException());
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.CreateRole(testableHttpRequestData);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateRole_WhenAddingUserFails_ShouldReturnBadRequest()
    {
        // Arrange
        const string roleName = "new-role";
        var createRoleRequest = new CreateRoleRequest
        {
            Name = roleName,
            AddToUsers = ["user1"]
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(createRoleRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                mockRoleRepository
                    .Setup(x => x.CreateRoleAsync(It.IsAny<string>(), It.IsAny<string?>()))
                    .ReturnsAsync(new Role { RoleName = roleName });
            })
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository
                    .Setup(x => x.AddRoleToUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ThrowsAsync(new RepositoryException("Failed to add user"));
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.CreateRole(testableHttpRequestData);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateRole_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        var createRoleRequest = new CreateRoleRequest { Name = "test-role" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithPayload(createRoleRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder().Build();

        // Act
        var result = await rolesHttpTrigger.CreateRole(testableHttpRequestData);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateRole_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var createRoleRequest = new CreateRoleRequest { Name = "test-role" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithInvalidAuthentication()
            .WithPayload(createRoleRequest)
            .Returns(HttpStatusCode.Unauthorized)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder().Build();

        // Act
        var result = await rolesHttpTrigger.CreateRole(testableHttpRequestData);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
