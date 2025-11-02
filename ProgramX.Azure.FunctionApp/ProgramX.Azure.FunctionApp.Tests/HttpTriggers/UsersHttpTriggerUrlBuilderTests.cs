using FluentAssertions;
using NUnit.Framework;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using ProgramX.Azure.FunctionApp.Tests.TestData;
using System.Reflection;
using System.Web;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;

[TestFixture]
public class UsersHttpTriggerUrlTests : TestBase
{
    private UsersHttpTrigger _usersHttpTrigger = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();
    }

    [Test]
    public void BuildPageUrl_WithBaseUrlOnly_ShouldReturnBaseUrl()
    {
        // Arrange
        var baseUrl = "https://api.example.com/users";

        // Act
        var result = InvokeBuildPageUrl(baseUrl, null, null, null, null, null, null);

        // Assert
        result.Should().Be(baseUrl);
    }

    [Test]
    public void BuildPageUrl_WithContainsText_ShouldIncludeEncodedParameter()
    {
        // Arrange
        var baseUrl = "https://api.example.com/users";
        var containsText = "john doe";

        // Act
        var result = InvokeBuildPageUrl(baseUrl, containsText, null, null, null, null, null);

        // Assert
        result.Should().Contain("containsText=");
        result.Should().Contain(Uri.EscapeDataString(containsText));
    }

    [Test]
    public void BuildPageUrl_WithRoles_ShouldIncludeRolesParameter()
    {
        // Arrange
        var baseUrl = "https://api.example.com/users";
        var withRoles = new[] { "Admin", "PowerUser" };

        // Act
        var result = InvokeBuildPageUrl(baseUrl, null, withRoles, null, null, null, null);

        // Assert
        result.Should().Contain("withRoles=");
        result.Should().Contain("Admin%2CPowerUser");
    }

    [Test]
    public void BuildPageUrl_WithApplications_ShouldIncludeApplicationsParameter()
    {
        // Arrange
        var baseUrl = "https://api.example.com/users";
        var hasAccessToApplications = new[] { "Dashboard", "Reports" };

        // Act
        var result = InvokeBuildPageUrl(baseUrl, null, null, hasAccessToApplications, null, null, null);

        // Assert
        result.Should().Contain("hasAccessToApplications=");
        result.Should().Contain("Dashboard%2CReports");
    }

    [Test]
    public void BuildPageUrl_WithOffsetAndItemsPerPage_ShouldIncludePaginationParameters()
    {
        // Arrange
        var baseUrl = "https://api.example.com/users";
        var offset = 20;
        var itemsPerPage = 10;

        // Act
        var result = InvokeBuildPageUrl(baseUrl, null, null, null, null, offset, itemsPerPage);

        // Assert
        result.Should().Contain("offset=20");
        result.Should().Contain("itemsPerPage=10");
    }

    [Test]
    public void BuildPageUrl_WithAllParameters_ShouldIncludeAllParameters()
    {
        // Arrange
        var baseUrl = "https://api.example.com/users";
        var containsText = "test user";
        var withRoles = new[] { "Admin" };
        var hasAccessToApplications = new[] { "Dashboard" };
        var continuationToken = "token123";
        var offset = 10;
        var itemsPerPage = 5;

        // Act
        var result = InvokeBuildPageUrl(baseUrl, containsText, withRoles, hasAccessToApplications, 
            continuationToken, offset, itemsPerPage);

        // Assert
        result.Should().StartWith(baseUrl);
        result.Should().Contain("containsText=");
        result.Should().Contain("withRoles=");
        result.Should().Contain("hasAccessToApplications=");
        result.Should().Contain("continuationToken=");
        result.Should().Contain("offset=10");
        result.Should().Contain("itemsPerPage=5");
    }

    [Test]
    public void BuildPageUrl_WithSpecialCharacters_ShouldEncodeCorrectly()
    {
        // Arrange
        var baseUrl = "https://api.example.com/users";
        var containsText = "user@domain.com & more";

        // Act
        var result = InvokeBuildPageUrl(baseUrl, containsText, null, null, null, null, null);

        // Assert
        result.Should().Contain("containsText=");
        result.Should().NotContain("@");
        result.Should().NotContain("&");
        result.Should().NotContain(" ");
    }

    private string InvokeBuildPageUrl(string baseUrl, string? containsText, 
        IEnumerable<string>? withRoles, IEnumerable<string>? hasAccessToApplications, 
        string? continuationToken, int? offset, int? itemsPerPage)
    {
        var method = typeof(UsersHttpTrigger).GetMethod("BuildPageUrl", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        return (string)method!.Invoke(_usersHttpTrigger, 
            new object?[] { baseUrl, containsText, withRoles, hasAccessToApplications, 
                continuationToken, offset, itemsPerPage })!;
    }
}
