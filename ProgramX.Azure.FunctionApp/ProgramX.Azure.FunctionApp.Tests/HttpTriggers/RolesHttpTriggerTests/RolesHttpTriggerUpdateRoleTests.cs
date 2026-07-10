using System.Net;
using FluentAssertions;
using Moq;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.RolesHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("RolesHttpTrigger")]
[Category("UpdateRole")]
[TestFixture]
public class RolesHttpTriggerUpdateRoleTests : TestBase
{
    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
    }

    [Test]
    public async Task UpdateRole_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        const string roleName = "test-role";
        var updateRoleRequest = new UpdateRoleRequest { Description = "updated description" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(updateRoleRequest)
            .Returns(HttpStatusCode.OK)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                mockRoleRepository
                    .Setup(x => x.UpdateRoleAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IEnumerable<string>?>()))
                    .ReturnsAsync(new Role { RoleName = roleName, Description = "updated description" });
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.UpdateRole(testableHttpRequestData, roleName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task UpdateRole_WithUsersInRole_ShouldCallUpdateWithUsers()
    {
        // Arrange
        const string roleName = "test-role";
        var updateRoleRequest = new UpdateRoleRequest
        {
            Description = "updated description",
            UsersInRole = ["user1", "user2"]
        };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(updateRoleRequest)
            .Returns(HttpStatusCode.OK)
            .Build();

        IEnumerable<string>? capturedUsers = null;
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                mockRoleRepository
                    .Setup(x => x.UpdateRoleAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IEnumerable<string>?>()))
                    .Callback<string, string?, IEnumerable<string>?>((_, _, u) => capturedUsers = u)
                    .ReturnsAsync(new Role { RoleName = roleName });
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.UpdateRole(testableHttpRequestData, roleName);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedUsers.Should().BeEquivalentTo(["user1", "user2"]);
    }

    [Test]
    public async Task UpdateRole_WithNullBody_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithBody("null")
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder().Build();

        // Act
        var result = await rolesHttpTrigger.UpdateRole(testableHttpRequestData, "some-role");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateRole_WhenRoleNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var updateRoleRequest = new UpdateRoleRequest { Description = "updated" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(updateRoleRequest)
            .Returns(HttpStatusCode.NotFound)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                mockRoleRepository
                    .Setup(x => x.UpdateRoleAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IEnumerable<string>?>()))
                    .ThrowsAsync(new ItemNotFoundException());
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.UpdateRole(testableHttpRequestData, "non-existent");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateRole_WhenImmutablePropertyUpdated_ShouldReturnBadRequest()
    {
        // Arrange
        var updateRoleRequest = new UpdateRoleRequest { Description = "updated" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(updateRoleRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                mockRoleRepository
                    .Setup(x => x.UpdateRoleAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IEnumerable<string>?>()))
                    .ThrowsAsync(new UpdateImmutablePropertyException());
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.UpdateRole(testableHttpRequestData, "test-role");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateRole_WhenUpdateFails_ShouldReturnBadRequest()
    {
        // Arrange
        var updateRoleRequest = new UpdateRoleRequest { Description = "updated" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithPayload(updateRoleRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                mockRoleRepository
                    .Setup(x => x.UpdateRoleAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IEnumerable<string>?>()))
                    .ThrowsAsync(new ItemUpdateException());
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.UpdateRole(testableHttpRequestData, "test-role");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateRole_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        var updateRoleRequest = new UpdateRoleRequest { Description = "updated" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithPayload(updateRoleRequest)
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder().Build();

        // Act
        var result = await rolesHttpTrigger.UpdateRole(testableHttpRequestData, "test-role");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateRole_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateRoleRequest = new UpdateRoleRequest { Description = "updated" };

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithInvalidAuthentication()
            .WithPayload(updateRoleRequest)
            .Returns(HttpStatusCode.Unauthorized)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder().Build();

        // Act
        var result = await rolesHttpTrigger.UpdateRole(testableHttpRequestData, "test-role");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
