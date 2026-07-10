using System.Collections.Specialized;
using System.Net;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.RolesHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("RolesHttpTrigger")]
[Category("GetRole")]
[TestFixture]
public class RolesHttpTriggerGetRoleTests
{
    [Test]
    public async Task GetRole_WithValidRoleName_ShouldReturnRoleWithUsersAndApplications()
    {
        // Arrange
        const string roleName = "test-role-123";

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.OK)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                var mockResult = new Mock<IResult<Role>>();
                mockResult.SetupGet(x => x.Items).Returns([
                    new Role { RoleName = roleName, Description = "Admin role" }
                ]);
                mockRoleRepository
                    .Setup(x => x.GetRolesAsync(It.IsAny<GetRolesCriteria>(), It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockResult.Object);
            })
            .WithIUserRepository(mockUserRepository =>
            {
                var mockUsersResult = new Mock<IResult<User>>();
                mockUsersResult.SetupGet(x => x.Items).Returns([
                    new User { Id = "1", UserName = "john", EmailAddress = "john@test.com", Roles = [roleName] }
                ]);
                mockUserRepository
                    .Setup(x => x.GetUsersAsync(It.IsAny<GetUsersCriteria>(), It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockUsersResult.Object);
            })
            .WithIApplicationProvider(mockAppProvider =>
            {
                var mockApp = new Mock<IApplication>();
                mockApp.Setup(x => x.GetApplicationMetaData()).Returns(new ApplicationMetaData
                {
                    Name = "TestApp",
                    FriendlyName = "Test App",
                    TargetUrl = "https://testapp.com",
                    RequiresRoleNames = [roleName]
                });
                mockAppProvider
                    .Setup(x => x.GetAllApplications(It.IsAny<GetAllApplicationsCriteria>()))
                    .Returns([mockApp.Object]);
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData, roleName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain(roleName);
        responseBody.Should().Contain("john");
    }

    [Test]
    public async Task GetRole_WithNonExistentRoleName_ShouldReturnNotFound()
    {
        // Arrange
        const string roleName = "non-existent-role";

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NotFound)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                var mockResult = new Mock<IResult<Role>>();
                mockResult.SetupGet(x => x.Items).Returns([]);
                mockRoleRepository
                    .Setup(x => x.GetRolesAsync(It.IsAny<GetRolesCriteria>(), It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData, roleName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetRole_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .Returns(HttpStatusCode.OK)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder().Build();

        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData, "some-role");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetRole_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithInvalidAuthentication()
            .Returns(HttpStatusCode.OK)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder().Build();

        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData, "some-role");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetRole_WithoutRoleName_ShouldReturnPagedRoles()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                var mockPagedResult = new Mock<IPagedResult<Role>>();
                mockPagedResult.SetupGet(x => x.Items).Returns([
                    new Role { RoleName = "role1", Description = "Role 1" },
                    new Role { RoleName = "role2", Description = "Role 2" }
                ]);
                mockPagedResult.SetupGet(x => x.NumberOfPages).Returns(1);
                mockRoleRepository
                    .Setup(x => x.GetRolesAsync(It.IsAny<GetRolesCriteria>(), It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockPagedResult.Object);
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData, null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain("role1");
        responseBody.Should().Contain("role2");
    }

    [Test]
    public async Task GetRole_WithContainsTextFilter_ShouldPassFilterToCriteria()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithQuery(new NameValueCollection { { "containsText", "admin" } })
            .Build();

        GetRolesCriteria? capturedCriteria = null;
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIRoleRepository(mockRoleRepository =>
            {
                var mockPagedResult = new Mock<IPagedResult<Role>>();
                mockPagedResult.SetupGet(x => x.Items).Returns([
                    new Role { RoleName = "admin-role", Description = "Admin" }
                ]);
                mockPagedResult.SetupGet(x => x.NumberOfPages).Returns(1);
                mockRoleRepository
                    .Setup(x => x.GetRolesAsync(It.IsAny<GetRolesCriteria>(), It.IsAny<PagedCriteria>()))
                    .Callback<GetRolesCriteria, PagedCriteria?>((c, _) => capturedCriteria = c)
                    .ReturnsAsync(mockPagedResult.Object);
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData, null);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedCriteria.Should().NotBeNull();
        capturedCriteria!.ContainingText.Should().Be("admin");
    }

    [Test]
    public async Task GetRole_WithoutAuthentication_AndNoRoleName_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .Returns(HttpStatusCode.OK)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder().Build();

        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData, null);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<string> GetResponseBodyAsync(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
    }
}
