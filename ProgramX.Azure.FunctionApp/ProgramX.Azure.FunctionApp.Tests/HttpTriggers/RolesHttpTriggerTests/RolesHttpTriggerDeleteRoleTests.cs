using System.Net;
using FluentAssertions;
using Moq;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.RolesHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("RolesHttpTrigger")]
[Category("DeleteRole")]
[TestFixture]
public class RolesHttpTriggerDeleteRoleTests
{
    [Test]
    public async Task DeleteRole_WhenRoleExists_ShouldReturnNoContent()
    {
        // Arrange
        const string roleName = "test-role";

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                mockRoleRepository
                    .Setup(x => x.DeleteRoleByNameAsync(It.IsAny<string>()))
                    .Returns(Task.CompletedTask);
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.DeleteRole(testableHttpRequestData, roleName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteRole_WhenRoleDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NotFound)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                mockRoleRepository
                    .Setup(x => x.DeleteRoleByNameAsync(It.IsAny<string>()))
                    .ThrowsAsync(new ItemNotFoundException());
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.DeleteRole(testableHttpRequestData, "does-not-exist");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteRole_WhenDeleteFails_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.BadRequest)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                mockRoleRepository
                    .Setup(x => x.DeleteRoleByNameAsync(It.IsAny<string>()))
                    .ThrowsAsync(new ItemUpdateException());
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.DeleteRole(testableHttpRequestData, "test-role");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task DeleteRole_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder().Build();

        // Act
        var result = await rolesHttpTrigger.DeleteRole(testableHttpRequestData, "test-role");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task DeleteRole_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithInvalidAuthentication()
            .Returns(HttpStatusCode.Unauthorized)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder().Build();

        // Act
        var result = await rolesHttpTrigger.DeleteRole(testableHttpRequestData, "test-role");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
