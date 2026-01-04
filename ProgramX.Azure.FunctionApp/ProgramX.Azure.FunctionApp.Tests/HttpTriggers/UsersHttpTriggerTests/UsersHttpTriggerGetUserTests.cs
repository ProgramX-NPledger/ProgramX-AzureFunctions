using System.Collections.Specialized;
using System.Net;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Tests.Mocks;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("GetUser")]
[TestFixture]
public class UsersHttpTriggerGetUserTests
{
    [Test]
    public async Task GetUser_WithValidId_ShouldReturnUserWithApplications()
    {
        // Arrange
        const string userId = "test-user-123";
        var adminRole = new Role
        {
            name = "Admin",
            applications = new List<Application>
            {
                new Application { name = "Dashboard",            metaDataDotNetAssembly = string.Empty,
                    metaDataDotNetType = string.Empty
                },
                new Application { name = "Reports",             metaDataDotNetAssembly = string.Empty,
                    metaDataDotNetType = string.Empty
                }
            }
        };

        var expectedUser = new User
        {
            id = userId,
            userName = "testuser",
            emailAddress = "test@example.com",
            firstName = "Test",
            lastName = "User",
            roles = new List<Role> { adminRole },
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(expectedUser);
            })
            .Build();
        
        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the response contains user and applications
        var responseBody = await TestHelpers.HttpBodyUtilities.GetResponseBodyAsync(result);
        responseBody.Should().Contain("testuser");
        responseBody.Should().Contain("Dashboard");
        responseBody.Should().Contain("Reports");
    }
    
    [Test]
    public async Task GetUser_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        const string userId = "non-existent-user";

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(null as User);
            })
            .Build();        
        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData, userId);
    
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Test]
    public async Task GetUser_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string userId = "test-user-123";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .Build();        
        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    
    [Test]
    public async Task GetUser_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string userId = "test-user-123";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithInvalidAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .Build();        
        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    
    
    [Test]
    public async Task GetUser_WithoutId_ShouldReturnPagedUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                id = "user1",
                emailAddress = "user1@example.com",
                userName = "user1",
                roles = new List<Role>()
            },
            new User
            {
                id = "user2",
                emailAddress = "user2@example.com",
                userName = "user2",
                roles = new List<Role>()
            }
        };
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Build();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockResult = new Mock<IPagedResult<User>>();
                mockResult.Setup(x => x.Items).Returns(users);
                
                mockUserRepository.Setup(x => x.GetUsersAsync(It.IsAny<GetUsersCriteria>(),It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();
        
        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData,null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var responseBody = await TestHelpers.HttpBodyUtilities.GetResponseBodyAsync(result);
        responseBody.Should().Contain("user1");
        responseBody.Should().Contain("user2");
    }
    
    [Test]
    public async Task GetUser_WithContainsTextFilter_ShouldReturnFilteredUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                id = "user1",
                emailAddress = "user1@example.com",
                userName = "john",
                roles = new List<Role>()
            },
            new User
            {
                id = "user2",
                emailAddress = "user2@example.com",
                userName = "user2",
                roles = new List<Role>()
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
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockResult = new Mock<IPagedResult<User>>();
                mockResult.Setup(x => x.Items).Returns(users.Where(q=>q.userName.Contains("john")));;
                
                mockUserRepository.Setup(x => x.GetUsersAsync(It.IsAny<GetUsersCriteria>(),It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData,null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var responseBody = await TestHelpers.HttpBodyUtilities.GetResponseBodyAsync(result);
        responseBody.Should().Contain("user1@example.com");
        responseBody.Should().NotContain("user2@example.com");
    }
    
    [Test]
    public async Task GetUser_WithRoleFilter_ShouldReturnUsersWithSpecificRoles()
    {
        // Arrange
        var adminRole = new Role { name = "Admin" };
        var guestRole = new Role { name = "Guest" };
        
        var users = new List<User>
        {
            new User
            {
                id = "user1",
                emailAddress = "user1@example.com",
                userName = "john",
                roles = new List<Role> { adminRole, guestRole }
            },
            new User
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
                var mockResult = new Mock<IPagedResult<User>>();
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
    
        var responseBody = await TestHelpers.HttpBodyUtilities.GetResponseBodyAsync(result);
        responseBody.Should().Contain("user1");
        responseBody.Should().NotContain("user2");
        
    }
    
    [Test]
    public async Task GetUser_WithApplicationFilter_ShouldReturnUsersWithAccessToSpecificApplications()
    {
         // Arrange
         var application1 = new Application { name = "Dashboard",             metaDataDotNetAssembly = string.Empty,
             metaDataDotNetType = string.Empty
         };
         var application2 = new Application { name = "Another App",             metaDataDotNetAssembly = string.Empty,
             metaDataDotNetType = string.Empty
         };
         var application3 = new Application { name = "Not this App",             metaDataDotNetAssembly = string.Empty,
             metaDataDotNetType = string.Empty
         };
         
        var adminRole = new Role
        {
            name = "Admin",
            applications = new List<Application> { application1, application2 }
        };
        var guestRole = new Role
        {
            name = "Guest",
            applications = new List<Application> { application1, application3 }
        };
        
        var users = new List<User>
        {
            new User
            {
                id = "user1",
                emailAddress = "user1@example.com",
                userName = "john",
                roles = new List<Role> { adminRole, guestRole }
            },
            new User
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
                    "hasAccessToApplications",
                    "Another App"
                }
            })
            .Build();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockResult = new Mock<IPagedResult<User>>();
                mockResult.Setup(x => x.Items).Returns(users.Where(q=>q.roles.Where((qq=>qq.applications.Any(qqq=>qqq.name == "Another App"))).Any()));;
                
                mockUserRepository.Setup(x => x.GetUsersAsync(It.IsAny<GetUsersCriteria>(),It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();

        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData,null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var responseBody = await TestHelpers.HttpBodyUtilities.GetResponseBodyAsync(result);
        responseBody.Should().Contain("Another App");
    }
    

    
}