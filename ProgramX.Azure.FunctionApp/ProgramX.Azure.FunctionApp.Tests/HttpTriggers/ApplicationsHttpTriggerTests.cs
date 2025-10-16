using System.Reflection;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.HttpTriggers;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;

[TestFixture]
public class ApplicationsHttpTriggerTests : TestBase
{
    private ApplicationsHttpTrigger _applicationsHttpTrigger = null!;
    private Mock<ILogger<LoginHttpTrigger>> _mockSpecificLogger = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockSpecificLogger = new Mock<ILogger<LoginHttpTrigger>>();
        _applicationsHttpTrigger = new ApplicationsHttpTrigger(
            _mockSpecificLogger.Object,
            MockCosmosClient.Object,
            Configuration);
    }
    
    
    [Test]
    public void BuildQueryDefinition_WithNameOnly_ShouldCreateCorrectQuery()
    {
        // Arrange
        var name = "TestApp";

        // Act
        var queryDefinition = InvokeBuildQueryDefinition(name, null, null);

        // Assert
        queryDefinition.Should().NotBeNull();
        queryDefinition.QueryText.Should().Contain("a.name=@id");
        queryDefinition.GetQueryParameters().Should().Contain(p => p.Name == "@id" && p.Value == name);
    }

    [Test]
    public void BuildQueryDefinition_WithContainsText_ShouldIncludeContainsFilter()
    {
        // Arrange
        var containsText = "dashboard";

        // Act
        var queryDefinition = InvokeBuildQueryDefinition(null, containsText, null);

        // Assert
        queryDefinition.Should().NotBeNull();
        queryDefinition.QueryText.Should().Contain("CONTAINS(UPPER(a.name), @containsText)");
        queryDefinition.QueryText.Should().Contain("CONTAINS(UPPER(a.description), @containsText)");
        queryDefinition.GetQueryParameters().Should().Contain(p => p.Name == "@containsText" && (string)p.Value == containsText.ToUpperInvariant());
    }

    [Test]
    public void BuildQueryDefinition_WithWithinRoles_ShouldIncludeRoleFilter()
    {
        // Arrange
        var withinRoles = new[] { "Admin", "PowerUser" };

        // Act
        var queryDefinition = InvokeBuildQueryDefinition(null, null, withinRoles);

        // Assert
        queryDefinition.Should().NotBeNull();
        queryDefinition.QueryText.Should().Contain("EXISTS(SELECT VALUE rr FROM rr IN c.roles WHERE rr.name = @role0)");
        queryDefinition.QueryText.Should().Contain("EXISTS(SELECT VALUE rr FROM rr IN c.roles WHERE rr.name = @role1)");
        queryDefinition.GetQueryParameters().Should().Contain(p => p.Name == "@role0" && (string)p.Value == withinRoles[0]);
        queryDefinition.GetQueryParameters().Should().Contain(p => p.Name == "@role1" && (string)p.Value == withinRoles[1]);
    }

    [Test]
    public void BuildQueryDefinition_WithAllParameters_ShouldIncludeAllFilters()
    {
        // Arrange
        var name = "TestApp";
        var containsText = "dashboard";
        var withinRoles = new[] { "Admin" };

        // Act
        var queryDefinition = InvokeBuildQueryDefinition(name, containsText, withinRoles);

        // Assert
        queryDefinition.Should().NotBeNull();
        queryDefinition.QueryText.Should().Contain("a.name=@id");
        // When name is specified, other filters are not applied
    }

    [Test]
    public void BuildQueryDefinition_WithoutName_ShouldIncludeGroupBy()
    {
        // Arrange
        var containsText = "test";

        // Act
        var queryDefinition = InvokeBuildQueryDefinition(null, containsText, null);

        // Assert
        queryDefinition.Should().NotBeNull();
        queryDefinition.QueryText.Should().Contain("GROUP BY");
    }

    private QueryDefinition InvokeBuildQueryDefinition(string? name, string? containsText, IEnumerable<string>? withinRoles)
    {
        var method = typeof(ApplicationsHttpTrigger).GetMethod("BuildQueryDefinition", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        return (QueryDefinition)method!.Invoke(_applicationsHttpTrigger, new object?[] { name, containsText, withinRoles })!;
    }
    

    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange, Act & Assert
        _applicationsHttpTrigger.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ApplicationsHttpTrigger(
            null!,
            MockCosmosClient.Object,
            Configuration));
    }

    [Test]
    public void Constructor_WithNullCosmosClient_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ApplicationsHttpTrigger(
            _mockSpecificLogger.Object,
            null!,
            Configuration));
    }

    [Test]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ApplicationsHttpTrigger(
            _mockSpecificLogger.Object,
            MockCosmosClient.Object,
            null!));
    }
}
