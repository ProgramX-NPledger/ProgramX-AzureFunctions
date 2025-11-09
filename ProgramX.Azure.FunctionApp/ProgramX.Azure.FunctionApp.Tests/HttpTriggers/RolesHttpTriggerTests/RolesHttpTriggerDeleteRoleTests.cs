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
[Category("RolesHttpTrigger")]
[Category("DeleteRole")]
[TestFixture]
public class RolesHttpTriggerDeletRoleTests
{
    [Test]
    public async Task DeleteUser_WhenUserExists_ShouldDeleteSuccessfully()
    {
        // Arrange
        const string roleName = "test-role-id";

        var existingRole = new Role
        {
            name = roleName,
            description = "test role description"
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetRoleByNameAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingRole);
                mockUserRepository.Setup(x => x.DeleteRoleByNameAsync(It.IsAny<string>()));
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
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NotFound)
            .Build();
        
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
                .WithIUserRepository(mockUserRepository =>
                {
                    mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                        .ReturnsAsync((SecureUser)null!);
                })
                .Build();

        // Act
        var result = await rolesHttpTrigger.DeleteRole(testableHttpRequestData, "does not exist");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteRole_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string userId = "test-role-id";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .Returns(HttpStatusCode.Unauthorized)
            .Build();        
        
        var roleshttpTrigger = new RolesHttpTriggerBuilder()
            .Build();

        // Act
        var result = await roleshttpTrigger.DeleteRole(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        // no header is added so it is a bad request
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    
    [Test]
    public async Task DeleteRole_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string roleName = "test-role-id";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithInvalidAuthentication()
            .Returns(HttpStatusCode.Unauthorized)
            .Build();        
        
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .Build();

        // Act
        var result = await rolesHttpTrigger.DeleteRole(testableHttpRequestData, roleName);

        // Assert
        result.Should().NotBeNull();
        // no header is added so it is a bad request
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

}
