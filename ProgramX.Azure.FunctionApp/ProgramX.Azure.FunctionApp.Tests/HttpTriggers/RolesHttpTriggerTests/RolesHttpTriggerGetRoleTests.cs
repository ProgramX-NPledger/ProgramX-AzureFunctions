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
    public async Task GetRole_WithValidId_ShouldReturnUserWithApplications()
    {
        // Arrange
        const string roleName = "test-role-123";
        var adminRole = new Role
        {
            name = roleName,
            description = "Admin role"
        };

        var expectedRole = new Role
        {
            name = roleName,
            description = "Admin role"
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.OK)
            .Build();
        
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockIResult = new Mock<IResult<SecureUser>>();
                mockIResult.SetupGet(x => x.Items)
                    .Returns(new List<SecureUser>
                    {
                        new SecureUser()
                        {
                            id = Guid.NewGuid().ToString(),
                            emailAddress = "hello@email.com",
                            userName = "hello",
                            roles = new List<Role>()
                            {
                                new Role()
                                {
                                    name = "Admin",
                                    applications = new List<Application>()
                                    {
                                        new Application
                                        {
                                            name = "AnApp",
                                            targetUrl = ""
                                        }
                                    }
                                }
                            },
                        },
                        new SecureUser()
                        {
                            id = Guid.NewGuid().ToString(),
                            emailAddress = "hello@email.com",
                            userName = "hello2",
                            roles = new List<Role>()
                            {
                                new Role()
                                {
                                    name = "AnotherRole",
                                    applications = new List<Application>()
                                    {
                                        new Application
                                        {
                                            name = "AnotherApp",
                                            targetUrl = ""
                                        }
                                    }
                                }
                            },
                        }
                    });
                
                mockUserRepository.Setup(x => x.GetUsersAsync(It.IsAny<GetUsersCriteria>(),It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockIResult.Object);
                mockUserRepository.Setup(x => x.GetRoleByNameAsync(It.IsAny<string>()))
                    .ReturnsAsync(expectedRole);
            })
            .Build();
        
        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData, roleName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the response contains user and applications
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain("AnApp");
        responseBody.Should().Contain("hello");
    }
    
    [Test]
    public async Task GetUser_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        const string roleName = "non-existent-user";

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(null as SecureUser);
            })
            .Build();        
        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData, roleName);
    
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Test]
    public async Task GetRole_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string roleName = "test-role-123";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .Build();        
        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData, roleName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    
    [Test]
    public async Task GetUser_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string roleName = "test-role-123";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithInvalidAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .Build();        
        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData, roleName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    
    
    [Test]
    public async Task GetRole_WithoutId_ShouldReturnPagedUsers()
    {
        // Arrange
        var roles = new List<Role>
        {
            new Role
            {
                name = "role1",
                description = "Role 1"
            },
            new Role
            {
                name = "role2",
                description = "Role 2"
            }
        };
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Build();
        
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockResult = new Mock<IPagedResult<Role>>();
                mockResult.Setup(x => x.Items).Returns(roles);
                
                mockUserRepository.Setup(x => x.GetRolesAsync(It.IsAny<GetRolesCriteria>(),It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();
        
        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData,null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain("role1");
        responseBody.Should().Contain("role2");
    }
    
    [Test]
    public async Task GetRole_WithContainsTextFilter_ShouldReturnFilteredRoles()
    {
        // Arrange
        var roles = new List<Role>
        {
            new Role
            {
                name = "role1",
                description = "Role 1"
            },
            new Role
            {
                name = "john",
                description = "Role 2"
            }
        };
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithQuery(new NameValueCollection()
            {
                {
                    "containsText",
                    "john"
                }
            })
            .Build();
        
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockResult = new Mock<IPagedResult<Role>>();
                mockResult.Setup(x => x.Items).Returns(roles.Where(q=>q.name.Contains("john")));;
                
                mockUserRepository.Setup(x => x.GetRolesAsync(It.IsAny<GetRolesCriteria>(),It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData,null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain("Role 2");
        responseBody.Should().NotContain("Role 1");
    }
    
    [Test]
    public async Task GetRole_WithRoleFilter_ShouldReturnUsersWithSpecificRoles()
    {
        // Arrange
        var adminRole = new Role { name = "Admin" };
        var guestRole = new Role { name = "Guest" };
        
        var users = new List<SecureUser>
        {
            new SecureUser
            {
                id = "user1",
                emailAddress = "user1@example.com",
                userName = "john",
                roles = new List<Role> { adminRole, guestRole }
            },
            new SecureUser
            {
                id = "user2",
                emailAddress = "user2@example.com",
                userName = "user2",
                roles = new List<Role> { guestRole }
            }
        };
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithQuery(new NameValueCollection()
            {
                {
                    "withRoles",
                    "Admin"
                }
            })
            .Build();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockResult = new Mock<IPagedResult<SecureUser>>();
                mockResult.Setup(x => x.Items).Returns(users.Where(q=>q.roles.Any(qq=>qq.name == "Admin")));;
                
                mockUserRepository.Setup(x => x.GetUsersAsync(It.IsAny<GetUsersCriteria>(),It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData,null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain("user1");
        responseBody.Should().NotContain("user2");
        
    }
    
    [Test]
    public async Task GetRole_WithApplicationFilter_ShouldReturnRolesWithAccessToSpecificApplications()
    {
        var roles = new List<Role>
        {
            new Role()
            {
                name = "role1",
                applications = new List<Application>
                {
                    new Application { name = "UsedInThisApp", targetUrl = "https://dashboard.example.com" },
                    new Application { name = "Another App", targetUrl = "https://dashboard.example.com" }
                }
            },
            new Role()
            {
                name = "role2",
                applications = new List<Application>
                {
                    new Application { name = "Dashboard", targetUrl = "https://dashboard.example.com" },
                    new Application { name = "Not this App", targetUrl = "https://dashboard.example.com" }
                }
            }
        };
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithQuery(new NameValueCollection()
            {
                {
                    "usedInApplications",
                    "UsedInThisApp"
                }
            })
            .Build();
        
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockResult = new Mock<IPagedResult<Role>>();
                mockResult.Setup(x => x.Items).Returns(roles.Where(q=>q.applications.Any(qq=>qq.name=="UsedInThisApp")));
                
                mockUserRepository.Setup(x => x.GetRolesAsync(It.IsAny<GetRolesCriteria>(),It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();

        // Act
        var result = await rolesHttpTrigger.GetRole(testableHttpRequestData,null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain("UsedInThisApp");
    }
    
    private async Task<string> GetResponseBodyAsync(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
    }
    
}