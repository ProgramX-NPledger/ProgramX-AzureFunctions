using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.ApplicationsHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("ApplicationsHttpTrigger")]
[Category("Ctor")]
[TestFixture]
public class ApplicationsHttpTriggerCtorTests
{
    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange, Act & Assert
        var mockedUserRepository = UserRepositoryFactory.CreateUserRepository();
        var mockedLogger = new Mock<ILogger<ApplicationsHttpTrigger>>();
        var mockedConfiguration = new Mock<IConfiguration>();

        var target = new ApplicationsHttpTrigger(mockedLogger.Object,
                mockedConfiguration.Object,
                mockedUserRepository.Object);
        Assert.That(target,Is.Not.Null);
    }

    [Test]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var mockedUserRepository = UserRepositoryFactory.CreateUserRepository();
        var mockedConfiguration = new Mock<IConfiguration>();
        
        Assert.Throws<ArgumentNullException>(() => new ApplicationsHttpTrigger(
            null!,
            mockedConfiguration.Object,
            mockedUserRepository.Object));
    }


    [Test]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var mockedLogger = new Mock<ILogger<ApplicationsHttpTrigger>>();
        var mockedUserRepository = UserRepositoryFactory.CreateUserRepository();
        
        Assert.Throws<ArgumentNullException>(() => new ApplicationsHttpTrigger(
            mockedLogger.Object,
            null!,
            mockedUserRepository.Object));;
    }
    
    [Test]
    public void Constructor_WithNullUserRepository_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var mockedLogger = new Mock<ILogger<ApplicationsHttpTrigger>>();
        var mockedConfiguration = new Mock<IConfiguration>();
        
        Assert.Throws<ArgumentNullException>(() => new ApplicationsHttpTrigger(
            mockedLogger.Object,
            mockedConfiguration.Object,
            null!));;
    }
}
