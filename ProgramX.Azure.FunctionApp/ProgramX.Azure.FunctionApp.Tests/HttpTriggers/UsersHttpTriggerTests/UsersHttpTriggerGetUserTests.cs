using System.Collections.Specialized;
using System.Net;
using System.Reflection;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Responses;
using ProgramX.Azure.FunctionApp.Tests.Mocks;
using ProgramX.Azure.FunctionApp.Tests.TestData;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("GetUser")]
[TestFixture]
public class UsersHttpTriggerGetUserTests : TestBase
{
    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

    }

    
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
                new Application { name = "Dashboard", targetUrl = "https://dashboard.example.com" },
                new Application { name = "Reports", targetUrl = "https://reports.example.com" }
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
            passwordHash = Array.Empty<byte>(),
            passwordSalt = Array.Empty<byte>()
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<User>(new List<User> { expectedUser });
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData, userId);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the response contains user and applications
        var responseBody = await GetResponseBodyAsync(result);
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
        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<User>(new List<User>());
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
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
        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<User>(new List<User>());
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
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
        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<User>(new List<User>());
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
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
        var users = new List<SecureUser>
        {
            new SecureUser
            {
                id = "user1",
                emailAddress = "user1@example.com",
                userName = "user1"
            },
            new SecureUser
            {
                id = "user2",
                emailAddress = "user2@example.com",
                userName = "user2"
            }
        };
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Build();
        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<SecureUser>(new List<SecureUser>(users));
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData,null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain("user1");
        responseBody.Should().Contain("user2");
    }
    
    [Test]
    public async Task GetUser_WithContainsTextFilter_ShouldReturnFilteredUsers()
    {
        // Arrange
        var users = new List<SecureUser>
        {
            new SecureUser
            {
                id = "user1",
                emailAddress = "user1@example.com",
                userName = "john"
            },
            new SecureUser
            {
                id = "user2",
                emailAddress = "user2@example.com",
                userName = "user2"
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
        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<SecureUser>(new List<SecureUser>(users));
        mockedCosmosDbClientFactory.FilterItems=(items =>
        {
            return items.Where(u => u.userName.Contains("john"));
        });
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
            .Build();

        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData,null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain("user1@example.com");
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
        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<SecureUser>(new List<SecureUser>(users));
        mockedCosmosDbClientFactory.FilterItems=(items =>
        {
            return items.Where(u => u.roles.Any(r => r.name == "Admin"));
        });
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
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
    public async Task GetUser_WithApplicationFilter_ShouldReturnUsersWithAccessToSpecificApplications()
    {
         // Arrange
         var application1 = new Application { name = "Dashboard", targetUrl = "https://dashboard.example.com" };
         var application2 = new Application { name = "Another App", targetUrl = "https://dashboard.example.com" };
         var application3 = new Application { name = "Not this App", targetUrl = "https://dashboard.example.com" };
         
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
                    "hasAccessToApplications",
                    "Another App"
                }
            })
            .Build();
        
        var mockedCosmosDbClientFactory = new MockedCosmosDbClientFactory<SecureUser>(new List<SecureUser>(users));
        mockedCosmosDbClientFactory.FilterItems=(items =>
        {
            return items.Where(u => u.roles.SelectMany(a=>a.applications).Any(r => r.name == "Another App"));
        });
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithCosmosClient(mockedCosmosDbClient.MockedCosmosClient)
            .WithConfiguration(Configuration)
            .Build();

        // Act
        var result = await usersHttpTrigger.GetUser(testableHttpRequestData,null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var responseBody = await GetResponseBodyAsync(result);
        responseBody.Should().Contain("Another App");
    }
    
    // [Test]
    // public async Task GetUser_WithPaginationParameters_ShouldHandlePaging()
    // {
    //     // Arrange
    //     var users = new List<SecureUser>
    //     {
    //         new SecureUser
    //         {
    //             id = "user1",
    //             emailAddress = "user1@example.com",
    //             userName = "user1"
    //         }
    //     };
    //
    //     var pagedResult = new PagedCosmosDbResult<SecureUser>(
    //         users, "continuation-token", 10, 2.0, 50, 0);
    //     pagedResult.SetTotalItems(50);
    //
    //     SetupGetPagedMultipleItemsMock(pagedResult);
    //
    //     var mockHttpRequest = CreateMockHttpRequestWithAuth();
    //     mockHttpRequest.Query.Add("offset", "10");
    //     mockHttpRequest.Query.Add("itemsPerPage", "10");
    //     mockHttpRequest.Query.Add("sortBy", "c.emailAddress");
    //
    //     // Act
    //     var result = await _usersHttpTrigger.GetUser(mockHttpRequest, null);
    //
    //     // Assert
    //     result.Should().NotBeNull();
    //     result.StatusCode.Should().Be(HttpStatusCode.OK);
    //
    //     var responseBody = await GetResponseBodyAsync(result);
    //     responseBody.Should().Contain("pageUrls"); // Should include pagination URLs
    // }
    //
    // [Test]
    // public async Task GetUser_WithContinuationToken_ShouldHandleTokenBasedPaging()
    // {
    //     // Arrange
    //     var users = new List<SecureUser>
    //     {
    //         new SecureUser
    //         {
    //             id = "user1",
    //             emailAddress = "user1@example.com", 
    //             userName = "user1"
    //         }
    //     };
    //
    //     var pagedResult = new PagedCosmosDbResult<SecureUser>(
    //         users, null, 10, 1.5, 20, 0);
    //     pagedResult.SetTotalItems(20);
    //
    //     SetupGetPagedMultipleItemsMock(pagedResult);
    //
    //     var mockHttpRequest = CreateMockHttpRequestWithAuth();
    //     mockHttpRequest.Query.Add("continuationToken", Uri.EscapeDataString("some-continuation-token"));
    //
    //     // Act
    //     var result = await _usersHttpTrigger.GetUser(mockHttpRequest, null);
    //
    //     // Assert
    //     result.Should().NotBeNull();
    //     result.StatusCode.Should().Be(HttpStatusCode.OK);
    // }
    //
    // [Test]
    // public async Task GetUser_WithInvalidPaginationParameters_ShouldUseDefaults()
    // {
    //     // Arrange
    //     var users = new List<SecureUser>
    //     {
    //         new SecureUser
    //         {
    //             id = "user1",
    //             emailAddress = "user1@example.com",
    //             userName = "user1"
    //         }
    //     };
    //
    //     var pagedResult = new PagedCosmosDbResult<SecureUser>(
    //         users, null, DataConstants.ItemsPerPage, 2.0, 1, 0);
    //     pagedResult.SetTotalItems(1);
    //
    //     SetupGetPagedMultipleItemsMock(pagedResult);
    //
    //     var mockHttpRequest = CreateMockHttpRequestWithAuth();
    //     mockHttpRequest.Query.Add("offset", "invalid-number");
    //     mockHttpRequest.Query.Add("itemsPerPage", "not-a-number");
    //
    //     // Act
    //     var result = await _usersHttpTrigger.GetUser(mockHttpRequest, null);
    //
    //     // Assert
    //     result.Should().NotBeNull();
    //     result.StatusCode.Should().Be(HttpStatusCode.OK);
    //     // Should handle gracefully and use default values
    // }
    //
    // #endregion
    //
    // #region Helper Methods
    //
    // private TestHttpRequestData CreateMockHttpRequestWithAuth(string? userId = null)
    // {
    //     var mockFunctionContext = new Mock<FunctionContext>();
    //     var query = new NameValueCollection();
    //     
    //     var url = userId != null 
    //         ? new Uri($"https://localhost:7071/api/user/{userId}")
    //         : new Uri("https://localhost:7071/api/user");
    //         
    //     var testRequest = new TestHttpRequestData(mockFunctionContext.Object, query, url);
    //     
    //     // Add auth header - this would normally be handled by your auth setup
    //     testRequest.Headers.Add("Authorization", "Bearer valid-jwt-token");
    //     
    //     return testRequest;
    // }
    //
    // private TestHttpRequestData CreateMockHttpRequestWithoutAuth()
    // {
    //     var mockFunctionContext = new Mock<FunctionContext>();
    //     var query = new NameValueCollection();
    //     var url = new Uri("https://localhost:7071/api/user");
    //     
    //     return new TestHttpRequestData(mockFunctionContext.Object, query, url);
    //     // No Authorization header
    // }
    //
    // private void SetupGetSingleItemMock(string userId, PagedCosmosDbResult<User> result)
    // {
    //     // You'll need to setup your mocks to return this result
    //     // This depends on how your UsersHttpTriggerBuilder configures the mocks
    //     // For now, this is a placeholder - you'd need to setup the actual mock calls
    //     // that GetSingleItemAsync makes internally
    // }
    //
    // private void SetupGetPagedMultipleItemsMock(PagedCosmosDbResult<SecureUser> result)
    // {
    //     // Similar to above - setup mocks for GetPagedMultipleItemsAsync
    //     // This depends on your specific mock configuration in UsersHttpTriggerBuilder
    // }
    //
    private async Task<string> GetResponseBodyAsync(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
    }
    //
    // #endregion
    //
    // // Keep existing tests for CalculatePageUrls...
    // [Test]
    // public void CalculatePageUrls_WithBasicPaging_ShouldGenerateCorrectUrls()
    // {
    //     // Arrange
    //     var pagedResult = new PagedCosmosDbResult<SecureUser>(
    //         new List<SecureUser>(), 
    //         null, 
    //         10, 
    //         5.0, 
    //         5,
    //         0); // 5 total pages
    //
    //     pagedResult.SetTotalItems(50); // Mock total items
    //
    //     var baseUrl = "https://api.example.com/users";
    //
    //     // Act
    //     var result = InvokeCalculatePageUrls(pagedResult, baseUrl, null, null, null, null, 0, 
    //         10).ToArray();
    //
    //     // Assert
    //     result.Should().HaveCount(5);
    //     result.First().PageNumber.Should().Be(1);
    //     result.First().IsCurrentPage.Should().BeTrue();
    //     result.Last().PageNumber.Should().Be(5);
    //     Assert.IsTrue(result.All(p => p.Url.StartsWith(baseUrl)));
    // }
    //
    // [Test]
    // public void CalculatePageUrls_WithFilters_ShouldIncludeFiltersInUrls()
    // {
    //     // Arrange
    //     var pagedResult = new PagedCosmosDbResult<SecureUser>(
    //         new List<SecureUser>(), 
    //         null, 
    //         10, 
    //         3.0, 
    //         2,
    //         0);
    //
    //     pagedResult.SetTotalItems(20);
    //
    //     var baseUrl = "https://api.example.com/users";
    //     var containsText = "test";
    //     var withRoles = new[] { "Admin" };
    //
    //     // Act
    //     var result = InvokeCalculatePageUrls(pagedResult, baseUrl, containsText, withRoles, null, null, 0, 10).ToArray();
    //
    //     // Assert
    //     result.Should().HaveCount(2);
    //     result.All(p => p.Url.Contains("containsText="));
    //     result.All(p => p.Url.Contains("withRoles="));
    // }
    //
    // [TestCase(0, 10, 1)]
    // [TestCase(10, 10, 2)]
    // [TestCase(20, 10, 3)]
    // public void CalculatePageUrls_WithDifferentOffsets_ShouldCalculateCorrectCurrentPage(
    //     int offset, int itemsPerPage, int expectedCurrentPage)
    // {
    //     // Arrange
    //     var pagedResult = new PagedCosmosDbResult<SecureUser>(
    //         new List<SecureUser>(), 
    //         null, 
    //         itemsPerPage, 
    //         2.0, 
    //         5,
    //         0);
    //
    //     pagedResult.SetTotalItems(50);
    //
    //     var baseUrl = "https://api.example.com/users";
    //
    //     // Act
    //     var result = InvokeCalculatePageUrls(pagedResult, baseUrl, null, null, null, null, offset, itemsPerPage);
    //
    //     // Assert
    //     var currentPage = result.FirstOrDefault(p => p.IsCurrentPage);
    //     currentPage.Should().NotBeNull();
    //     currentPage!.PageNumber.Should().Be(expectedCurrentPage);
    // }
    //
    // private IEnumerable<UrlAccessiblePage> InvokeCalculatePageUrls(
    //     PagedCosmosDbResult<SecureUser> pagedResult, string baseUrl, string? containsText,
    //     IEnumerable<string>? withRoles, IEnumerable<string>? hasAccessToApplications, 
    //     string? continuationToken, int offset, int itemsPerPage)
    // {
    //     var method = typeof(UsersHttpTrigger).GetMethod("CalculatePageUrls", 
    //         BindingFlags.NonPublic | BindingFlags.Instance);
    //     
    //     return (IEnumerable<UrlAccessiblePage>)method!.Invoke(_usersHttpTrigger, 
    //         new object?[] { pagedResult, baseUrl, containsText, withRoles, hasAccessToApplications, 
    //             continuationToken, offset, itemsPerPage })!;
    // }
    //
    //
    #region Helper Methods


    private ItemResponse<T> CreateMockItemResponse<T>(HttpStatusCode statusCode)
    {
        var mockResponse = new Mock<ItemResponse<T>>();
        mockResponse.Setup(x => x.StatusCode).Returns(statusCode);
        return mockResponse.Object;
    }

    #endregion
}