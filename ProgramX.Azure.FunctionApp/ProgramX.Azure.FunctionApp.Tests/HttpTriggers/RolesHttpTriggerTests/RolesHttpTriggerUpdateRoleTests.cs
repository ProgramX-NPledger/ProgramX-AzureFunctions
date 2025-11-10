using System.Net;
using FluentAssertions;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.RolesHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("UpdateUser")]
[TestFixture]
public class RolesHttpTriggerUpdateRoleTests : TestBase
{
    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

    }

    
    [Test]
    public async Task UpdateRole_WithValidId_ShouldReturnOkAndUserShouldBeUpdated()
    {
        // Arrange
        const string roleName = "test-role-123";
        const string expectedDescription = "updated description";

        var existingRole = new Role()
        {
            name = "existingRole",
            description = "existing description"
        };
        var updateRoleRequest = new UpdateRoleRequest()
        {
            decription = expectedDescription,
            name = roleName
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateRoleRequest)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockApplicationsResult = new Mock<IResult<Application>>();
                mockApplicationsResult.SetupGet(x => x.Items).Returns(new List<Application>());
                
                mockUserRepository.Setup(x =>
                    x.GetApplicationsAsync(It.IsAny<GetApplicationsCriteria>(), It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockApplicationsResult.Object);
                mockUserRepository.Setup(x => x.GetRoleByNameAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingRole);
                mockUserRepository.Setup(x => x.UpdateRoleAsync(It.IsAny<string>(),It.IsAny<Role>()));
            })
            .Build();
        
        // Act
        var result = await rolesHttpTrigger.UpdateRole(testableHttpRequestData, roleName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        
    }
    
    
    
    [Test]
    public async Task UpdateRole_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        const string roleName = "test-role-123";

        var updateRole = new UpdateRoleRequest()
        {
            name = roleName,
            decription = "updated description"
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateRole)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await rolesHttpTrigger.UpdateRole(testableHttpRequestData, roleName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);

    }
    
    [Test]
    public async Task UpdateRole_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        const string roleName = "test-role-123";
        
        var updateRole = new UpdateRoleRequest()
        {
            name = roleName,
            decription = "updated description"
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithPayload(updateRole)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await rolesHttpTrigger.UpdateRole(testableHttpRequestData, roleName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }
    
    
    [Test]
    public async Task UpdateRole_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string roleName = "test-role-123";

        var updateRole = new UpdateRoleRequest()
        {
            name = roleName,
            decription = "updated description"
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithInvalidAuthentication()
            .WithPayload(updateRole)
            .Returns(HttpStatusCode.Unauthorized)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await rolesHttpTrigger.UpdateRole(testableHttpRequestData, roleName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);;

    }
    
}