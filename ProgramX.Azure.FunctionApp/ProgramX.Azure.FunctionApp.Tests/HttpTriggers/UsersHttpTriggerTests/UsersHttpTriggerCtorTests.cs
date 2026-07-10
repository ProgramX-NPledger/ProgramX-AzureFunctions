using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("Ctor")]
[TestFixture]
public class UsersHttpTriggerCtorTests : TestBase
{
    [SetUp]
    public override void SetUp() => base.SetUp();

    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange, Act
        var usersHttpTrigger = new UsersHttpTriggerBuilder().Build();

        // Assert
        usersHttpTrigger.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithCustomLogger_ShouldCreateInstance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<UsersHttpTrigger>>();

        // Act
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithLogger(mockLogger)
            .Build();

        // Assert
        usersHttpTrigger.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithConfiguration_ShouldCreateInstance()
    {
        // Arrange, Act
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();

        // Assert
        usersHttpTrigger.Should().NotBeNull();
    }
}
