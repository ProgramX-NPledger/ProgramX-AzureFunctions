using System.Collections.Specialized;
using System.Net;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.ApplicationsHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("ApplicationsHttpTrigger")]
[Category("GetApplication")]
[TestFixture]
public class ApplicationsHttpTriggerGetApplicationTests
{
    [Test]
    public async Task GetApplication_WithValidName_ShouldSucceed()
    {
        // Arrange
        var expectedApplication = new Application
        {
            name = "application",
            targetUrl = ""
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetApplicationByNameAsync(It.IsAny<string>()))
                    .ReturnsAsync(expectedApplication);
            })
            .Build();
        
        // Act
        var result = await applicationsHttpTrigger.GetApplication(testableHttpRequestData, expectedApplication.name);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the response contains user and applications
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain(expectedApplication.name);
    }
    
    [Test]
    public async Task GetApplication_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        const string applicationName = "non-existent-application";

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetApplicationByNameAsync(It.IsAny<string>()))
                    .ReturnsAsync((Application)null!);
            })
            .Build();
        
        // Act
        var result = await applicationsHttpTrigger.GetApplication(testableHttpRequestData, applicationName);
    
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Test]
    public async Task GetApplication_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string applicationName = "test-application-123";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var applicationHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .Build();        
        
        // Act
        var result = await applicationHttpTrigger.GetApplication(testableHttpRequestData, applicationName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    
    [Test]
    public async Task GetApplication_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string applicationName = "test-application-123";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithInvalidAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .Build();        
        
        // Act
        var result = await applicationsHttpTrigger.GetApplication(testableHttpRequestData, applicationName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    
    [Test]
    public async Task GetApplication_WithoutId_ShouldReturnPagedApplications()
    {
        // Arrange
        var users = new List<SecureUser>
        {
            new SecureUser
            {
                id = "user1",
                emailAddress = "user1@example.com",
                userName = "user1",
                roles = new List<Role>
                {
                    new Role()
                    {
                        applications = new List<Application>()
                        {
                            new Application
                            {
                                name = "Admin Application",
                                targetUrl = ""
                            }
                        }
                    }
                }
            },
            new SecureUser
            {
                id = "user2",
                emailAddress = "user2@example.com",
                userName = "user2",
                roles = new List<Role>
                {
                    new Role()
                    {
                        applications = new List<Application>()
                        {
                            new Application
                            {
                                name = "A different Application",
                                targetUrl = ""
                            }
                        }
                    }
                }
            }
        };
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Build();
        
        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockResult = new Mock<IPagedResult<Application>>();
                mockResult.SetupGet(x => x.Items)
                    .Returns(
                        users.SelectMany(q =>
                            q.roles.SelectMany(qq =>
                                qq.applications
                            )
                        ));
                
                mockUserRepository.Setup(x => x.GetApplicationsAsync(It.IsAny<GetApplicationsCriteria>(),It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();
        
        // Act
        var result = await applicationsHttpTrigger.GetApplication(testableHttpRequestData,null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain("Admin Application");
        
    }
    
    [Test]
    public async Task GetApplication_WithContainsTextFilter_ShouldReturnFilteredApplications()
    {
        // Arrange
        var users = new List<SecureUser>
        {
            new SecureUser
            {
                id = "user1",
                emailAddress = "user1@example.com",
                userName = "john",
                roles = new List<Role>()
                {
                    new Role()
                    {
                        name = "Admin",
                        applications = new List<Application>()
                        {
                            new Application
                            {
                                name = "Admin Application",
                                targetUrl = ""
                            }
                        }
                    }
                }
            },
            new SecureUser
            {
                id = "user2",
                emailAddress = "user2@example.com",
                userName = "user2",
                roles = new List<Role>()
                {
                    new Role()
                    {
                        name = "Admin",
                        applications = new List<Application>()
                        {
                            new Application
                            {
                                name = "john",
                                targetUrl = ""
                            }
                        }
                    }
                }
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
        
        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockResult = new Mock<IPagedResult<Application>>();
                mockResult.SetupGet(x => x.Items)
                    .Returns(
                        users.SelectMany(q =>
                            q.roles.SelectMany(qq =>
                                qq.applications.Where(qqq =>
                                    qqq.name.Contains("john")
                                )
                            )
                        ));
                
                mockUserRepository.Setup(x => x.GetApplicationsAsync(It.IsAny<GetApplicationsCriteria>(),It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();

        // Act
        var result = await applicationsHttpTrigger.GetApplication(testableHttpRequestData,null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain("john");
        responseBody.Should().NotContain("user2@example.com");
    }
    
    [Test]
    public async Task GetUser_WithRoleFilter_ShouldReturnUsersWithSpecificRoles()
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
                roles = new List<Role>()
                {
                    new Role()
                    {
                        name = "Role1",
                        applications = new List<Application>()
                        {
                            new Application
                            {
                                name = "Admin Application",
                                targetUrl = ""
                            }
                        }
                    }
                }
            },
            new SecureUser
            {
                id = "user2",
                emailAddress = "user2@example.com",
                userName = "user2",
                roles = new List<Role>()
                {
                    new Role()
                    {
                        name = "Role2",
                        applications = new List<Application>()
                        {
                            new Application
                            {
                                name = "john",
                                targetUrl = ""
                            }
                        }
                    }
                }
            }
        };
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithQuery(new NameValueCollection()
            {
                {
                    "withRoles",
                    "Role1"
                }
            })
            .Build();
        
        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockResult = new Mock<IPagedResult<Application>>();
                mockResult.SetupGet(x => x.Items)
                    .Returns(
                        users.SelectMany(q =>
                            q.roles.SelectMany(qq =>
                                qq.applications.Where(qqq =>
                                    qqq.name.Contains("Admin Application")
                                )
                            )
                        ));
                
                mockUserRepository.Setup(x => x.GetApplicationsAsync(It.IsAny<GetApplicationsCriteria>(),It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockResult.Object);
            })
            .Build();

        // Act
        var result = await applicationsHttpTrigger.GetApplication(testableHttpRequestData,null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain("Admin Application");
        responseBody.Should().NotContain("john");
        
    }
    
    private async Task<string> GetResponseBodyAsync(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
    }
    
}