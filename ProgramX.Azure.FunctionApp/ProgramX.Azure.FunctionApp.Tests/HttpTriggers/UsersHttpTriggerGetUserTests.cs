using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Responses;
using ProgramX.Azure.FunctionApp.Tests.TestData;
using System.Collections.Specialized;
using System.Net;
using System.Reflection;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;

[TestFixture]
public class UsersHttpTriggerGetUserTests : TestBase
{
    private UsersHttpTrigger _usersHttpTrigger = null!;
    private Mock<PagedCosmosDbReader<SecureUser>> _mockSecureUserReader = null!;
    private Mock<PagedCosmosDbReader<User>> _mockUserReader = null!;
    // private IPagedReader<SecureUser> _iPagedSecureUserReader = null!;
    // private IPagedReader<User> _iPagedUserReader = null!;
    private Mock<HttpRequestData> _mockHttpRequestData = null!;
    private NameValueCollection _mockQuery = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        
        _mockSecureUserReader = new Mock<PagedCosmosDbReader<SecureUser>>(
            MockCosmosClient.Object, 
            DataConstants.CoreDatabaseName, 
            DataConstants.UsersContainerName, 
            DataConstants.UserNamePartitionKeyPath);
        
        _mockUserReader = new Mock<PagedCosmosDbReader<User>>(
            MockCosmosClient.Object, 
            DataConstants.CoreDatabaseName, 
            DataConstants.UsersContainerName, 
            DataConstants.UserNamePartitionKeyPath);
        
        _mockHttpRequestData = new Mock<HttpRequestData>();
        _mockQuery = new NameValueCollection();
        
        _mockHttpRequestData.Setup(x => x.Query).Returns(_mockQuery);
        _mockHttpRequestData.Setup(x => x.Url).Returns(new Uri("https://localhost:7071/api/user"));

        _usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithDefaultMocks()
            .WithConfiguration(Configuration)
            .Build();
    }
    //
    // [Test]
    // public async Task GetSingleItemAsync_WithValidId_ShouldReturnUser()
    // {
    //     // Arrange
    //     var userId = "test-user-123";
    //     var expectedUser = new User
    //     {
    //         id = userId,
    //         userName = "testuser",
    //         emailAddress = "test@example.com",
    //         firstName = "Test",
    //         lastName = "User",
    //         passwordHash = [],
    //         passwordSalt = []
    //     };
    //
    //     var pagedResult = new PagedCosmosDbResult<User>(
    //         new List<User> { expectedUser },
    //         null,
    //         1,
    //         2.5,
    //         1,
    //         0);
    //
    //     _mockUserReader
    //         .Setup(x => x.GetPagedItemsAsync(It.IsAny<QueryDefinition>(), "c.id", null, null))
    //         .ReturnsAsync(pagedResult);
    //
    //     // Act
    //     var result = await InvokeGetSingleItemAsync(userId);
    //
    //     // Assert
    //     result.Should().NotBeNull();
    //     result!.id.Should().Be(userId);
    //     result.userName.Should().Be("testuser");
    //     result.emailAddress.Should().Be("test@example.com");
    // }
    //
    // [Test]
    // public async Task GetSingleItemAsync_WithNonExistentId_ShouldReturnNull()
    // {
    //     // Arrange
    //     var userId = "non-existent-user";
    //     var pagedResult = new PagedCosmosDbResult<User>(
    //         new List<User>(),
    //         null,
    //         1,
    //         1.0,
    //         0,
    //         0);
    //
    //     _mockUserReader
    //         .Setup(x => x.GetPagedItemsAsync(It.IsAny<QueryDefinition>(), "c.id", null, null))
    //         .ReturnsAsync(pagedResult);
    //
    //     // Act
    //     var result = await InvokeGetSingleItemAsync(userId);
    //
    //     // Assert
    //     result.Should().BeNull();
    // }
    //
    // [Test]
    // public async Task GetPagedMultipleItemsAsync_WithNoFilters_ShouldReturnAllUsers()
    // {
    //     // Arrange
    //     var users = new List<SecureUser>
    //     {
    //         new SecureUser()
    //         {
    //             id = string.Empty,
    //             emailAddress = "user1@example.com",
    //             userName = "user1" 
    //         },
    //         new SecureUser()
    //         {
    //             id = string.Empty,
    //             emailAddress = "user2@example.com",
    //             userName = "user2" 
    //         }
    //     };
    //
    //     var pagedResult = new PagedCosmosDbResult<SecureUser>(
    //         users,
    //         null,
    //         DataConstants.ItemsPerPage,
    //         5.0,
    //         1,
    //         0);
    //
    //     _mockSecureUserReader
    //         .Setup(x => x.GetPagedItemsAsync(It.IsAny<QueryDefinition>(), "c.userName", 0, DataConstants.ItemsPerPage))
    //         .ReturnsAsync(pagedResult);
    //
    //     // Act
    //     var result = await InvokeGetPagedMultipleItemsAsync(null, null, null, "c.userName", 0, DataConstants.ItemsPerPage);
    //
    //     // Assert
    //     result.Should().NotBeNull();
    //     result.Items.Should().HaveCount(2);
    //     result.Items.First().userName.Should().Be("user1");
    //     result.Items.Last().userName.Should().Be("user2");
    // }
    //
    // [Test]
    // public async Task GetPagedMultipleItemsAsync_WithContainsTextFilter_ShouldReturnFilteredUsers()
    // {
    //     // Arrange
    //     var containsText = "john";
    //     var users = new List<SecureUser>
    //     {
    //         new SecureUser()
    //         {
    //             id = string.Empty,
    //             emailAddress = "john@example.com",
    //             userName = "john.doe"
    //         }
    //     };
    //
    //     var pagedResult = new PagedCosmosDbResult<SecureUser>(
    //         users,
    //         null,
    //         DataConstants.ItemsPerPage,
    //         3.2,
    //         1,
    //         0);
    //
    //     _mockSecureUserReader
    //         .Setup(x => x.GetPagedItemsAsync(It.IsAny<QueryDefinition>(), "c.userName", 0, DataConstants.ItemsPerPage))
    //         .ReturnsAsync(pagedResult);
    //
    //     // Act
    //     var result = await InvokeGetPagedMultipleItemsAsync(containsText, null, null, "c.userName", 0, DataConstants.ItemsPerPage);
    //
    //     // Assert
    //     result.Should().NotBeNull();
    //     result.Items.Should().HaveCount(1);
    //     result.Items.First().userName.Should().Be("john.doe");
    // }
    //
    // [Test]
    // public async Task GetPagedMultipleItemsAsync_WithRoleFilter_ShouldReturnUsersWithRoles()
    // {
    //     // Arrange
    //     var withRoles = new[] { "Admin", "PowerUser" };
    //     var adminRole = new Role()
    //     {
    //         name = "Admin"
    //     };
    //     var users = new List<SecureUser>
    //     {
    //         new SecureUser
    //         {
    //             id = string.Empty,
    //             userName = "admin.user",
    //             roles =
    //             [
    //                 adminRole
    //             ],
    //             emailAddress = string.Empty
    //         }
    //     };
    //
    //     var pagedResult = new PagedCosmosDbResult<SecureUser>(
    //         users,
    //         null,
    //         DataConstants.ItemsPerPage,
    //         4.1,
    //         1,
    //         0);
    //
    //     _mockSecureUserReader
    //         .Setup(x => x.GetPagedItemsAsync(It.IsAny<QueryDefinition>(), "c.userName", 0, DataConstants.ItemsPerPage))
    //         .ReturnsAsync(pagedResult);
    //
    //     // Act
    //     var result = await InvokeGetPagedMultipleItemsAsync(null, withRoles, null, "c.userName", 0, DataConstants.ItemsPerPage);
    //
    //     // Assert
    //     result.Should().NotBeNull();
    //     result.Items.Should().HaveCount(1);
    //     result.Items.First().roles.Should().Contain(r => r.name == "Admin");
    // }
    //
    // [Test]
    // public async Task GetPagedMultipleItemsAsync_WithApplicationFilter_ShouldReturnUsersWithApplicationAccess()
    // {
    //     // Arrange
    //     var hasAccessToApplications = new[] { "Dashboard", "Reports" };
    //     var application = new Application()
    //     {
    //         name = "Dashboard",
    //         targetUrl = string.Empty
    //     };
    //     var role = new Role()
    //     {
    //         name = "User",
    //         applications =
    //             [application]
    //     };
    //     var users = new List<SecureUser>
    //     {
    //         new SecureUser()
    //         {
    //             id = string.Empty,
    //             emailAddress = string.Empty,
    //             userName = "dashboard.user",
    //             roles = [role]
    //         }
    //     };
    //
    //     var pagedResult = new PagedCosmosDbResult<SecureUser>(
    //         users,
    //         null,
    //         DataConstants.ItemsPerPage,
    //         3.8,
    //         1,
    //         0);
    //
    //     _mockSecureUserReader
    //         .Setup(x => x.GetPagedItemsAsync(It.IsAny<QueryDefinition>(), "c.userName", 0, DataConstants.ItemsPerPage))
    //         .ReturnsAsync(pagedResult);
    //
    //     // Act
    //     var result = await InvokeGetPagedMultipleItemsAsync(null, null, hasAccessToApplications, "c.userName", 0, DataConstants.ItemsPerPage);
    //
    //     // Assert
    //     result.Should().NotBeNull();
    //     result.Items.Should().HaveCount(1);
    //     result.Items.First().userName.Should().Be("dashboard.user");
    // }

    [Test]
    public void CalculatePageUrls_WithBasicPaging_ShouldGenerateCorrectUrls()
    {
        // Arrange
        var pagedResult = new PagedCosmosDbResult<SecureUser>(
            new List<SecureUser>(), 
            null, 
            10, 
            5.0, 
            5,
            0); // 5 total pages

        pagedResult.SetTotalItems(50); // Mock total items

        var baseUrl = "https://api.example.com/users";

        // Act
        var result = InvokeCalculatePageUrls(pagedResult, baseUrl, null, null, null, null, 0, 
            10).ToArray();

        // Assert
        result.Should().HaveCount(5);
        result.First().PageNumber.Should().Be(1);
        result.First().IsCurrentPage.Should().BeTrue();
        result.Last().PageNumber.Should().Be(5);
        Assert.IsTrue(result.All(p => p.Url.StartsWith(baseUrl)));
    }

    [Test]
    public void CalculatePageUrls_WithFilters_ShouldIncludeFiltersInUrls()
    {
        // Arrange
        var pagedResult = new PagedCosmosDbResult<SecureUser>(
            new List<SecureUser>(), 
            null, 
            10, 
            3.0, 
            2,
            0);

        pagedResult.SetTotalItems(20);

        var baseUrl = "https://api.example.com/users";
        var containsText = "test";
        var withRoles = new[] { "Admin" };

        // Act
        var result = InvokeCalculatePageUrls(pagedResult, baseUrl, containsText, withRoles, null, null, 0, 10).ToArray();

        // Assert
        result.Should().HaveCount(2);
        result.All(p => p.Url.Contains("containsText="));
        result.All(p => p.Url.Contains("withRoles="));
    }

    [TestCase(0, 10, 1)]
    [TestCase(10, 10, 2)]
    [TestCase(20, 10, 3)]
    public void CalculatePageUrls_WithDifferentOffsets_ShouldCalculateCorrectCurrentPage(
        int offset, int itemsPerPage, int expectedCurrentPage)
    {
        // Arrange
        var pagedResult = new PagedCosmosDbResult<SecureUser>(
            new List<SecureUser>(), 
            null, 
            itemsPerPage, 
            2.0, 
            5,
            0);

        pagedResult.SetTotalItems(50);

        var baseUrl = "https://api.example.com/users";

        // Act
        var result = InvokeCalculatePageUrls(pagedResult, baseUrl, null, null, null, null, offset, itemsPerPage);

        // Assert
        var currentPage = result.FirstOrDefault(p => p.IsCurrentPage);
        currentPage.Should().NotBeNull();
        currentPage!.PageNumber.Should().Be(expectedCurrentPage);
    }

    // Helper methods using reflection to access private methods
    private async Task<User?> InvokeGetSingleItemAsync(string name)
    {
        var method = typeof(UsersHttpTrigger).GetMethod("GetSingleItemAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var task = (Task<User?>)method!.Invoke(_usersHttpTrigger, new object[] { name })!;
        return await task;
    }

    private async Task<PagedCosmosDbResult<SecureUser>> InvokeGetPagedMultipleItemsAsync(
        string? containsText, string[]? withRoles, string[]? hasAccessToApplications, 
        string sortByColumn, int? offset, int? itemsPerPage)
    {
        var method = typeof(UsersHttpTrigger).GetMethod("GetPagedMultipleItemsAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var task = (Task<PagedCosmosDbResult<SecureUser>>)method!.Invoke(_usersHttpTrigger, 
            new object?[] { containsText, withRoles, hasAccessToApplications, sortByColumn, offset, itemsPerPage })!;
        
        return await task;
    }

    private IEnumerable<UrlAccessiblePage> InvokeCalculatePageUrls(
        PagedCosmosDbResult<SecureUser> pagedResult, string baseUrl, string? containsText,
        IEnumerable<string>? withRoles, IEnumerable<string>? hasAccessToApplications, 
        string? continuationToken, int offset, int itemsPerPage)
    {
        var method = typeof(UsersHttpTrigger).GetMethod("CalculatePageUrls", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        return (IEnumerable<UrlAccessiblePage>)method!.Invoke(_usersHttpTrigger, 
            new object?[] { pagedResult, baseUrl, containsText, withRoles, hasAccessToApplications, 
                continuationToken, offset, itemsPerPage })!;
    }
}
