using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.RolesHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("RolesHttpTrigger")]
[Category("Ctor")]
[TestFixture]
public class RolesHttpTriggerCtorTests : TestBase
{
    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
    }

    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange, Act
        var rolesHttpTrigger = new RolesHttpTriggerBuilder().Build();

        // Assert
        rolesHttpTrigger.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithCustomLogger_ShouldCreateInstance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<RolesHttpTrigger>>();

        // Act
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithLogger(mockLogger)
            .Build();

        // Assert
        rolesHttpTrigger.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithConfiguration_ShouldCreateInstance()
    {
        // Arrange, Act
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();

        // Assert
        rolesHttpTrigger.Should().NotBeNull();
    }
}
