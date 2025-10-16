using ProgramX.Azure.FunctionApp.Model;
using FluentAssertions;
using NUnit.Framework;

namespace ProgramX.Azure.FunctionApp.Tests.Model;

[TestFixture]
public class ApplicationTests
{
    [Test]
    public void Application_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var application = new Application
        {
            name = "TestApp",
            targetUrl = "https://test.com"
        };

        // Assert
        application.type.Should().Be("application");
        application.schemaVersionNumber.Should().Be(1);
        application.isDefaultApplicationOnLogin.Should().BeFalse();
        application.ordinal.Should().Be(0);
    }

    [Test]
    public void Application_ShouldAllowSettingProperties()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow.AddHours(1);

        // Act
        var application = new Application
        {
            name = "TestApplication",
            description = "Test Description",
            imageUrl = "https://example.com/image.png",
            targetUrl = "https://example.com",
            isDefaultApplicationOnLogin = true,
            ordinal = 5,
            createdAt = createdAt,
            updatedAt = updatedAt
        };

        // Assert
        application.name.Should().Be("TestApplication");
        application.description.Should().Be("Test Description");
        application.imageUrl.Should().Be("https://example.com/image.png");
        application.targetUrl.Should().Be("https://example.com");
        application.isDefaultApplicationOnLogin.Should().BeTrue();
        application.ordinal.Should().Be(5);
        application.createdAt.Should().Be(createdAt);
        application.updatedAt.Should().Be(updatedAt);
    }

    // [Test]
    // public void Application_RequiredProperties_ShouldThrowWhenNotSet()
    // {
    //     // Arrange & Act & Assert
    //     Assert.Throws<ArgumentException>(() => new Application
    //     {
    //         // Missing required 'name' property
    //         targetUrl = "https://test.com"
    //     });
    // }

    [TestCase("Dashboard", "https://dashboard.com")]
    [TestCase("Reports", "https://reports.com")]
    [TestCase("Settings", "https://settings.com")]
    public void Application_ShouldAcceptValidNameAndUrl(string name, string url)
    {
        // Arrange & Act
        var application = new Application
        {
            name = name,
            targetUrl = url
        };

        // Assert
        application.name.Should().Be(name);
        application.targetUrl.Should().Be(url);
    }
}
