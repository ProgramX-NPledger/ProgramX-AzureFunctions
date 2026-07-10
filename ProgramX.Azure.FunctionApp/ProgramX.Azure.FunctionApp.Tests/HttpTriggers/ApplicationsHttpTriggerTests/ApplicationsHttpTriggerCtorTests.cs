using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
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
        var mockedUserRepository = new Mock<IUserRepository>();
        var mockedLogger = new Mock<ILogger<ApplicationsHttpTrigger>>();
        var mockedConfiguration = new Mock<IConfiguration>();
        var mockedServiceProvider = new Mock<IServiceProvider>();
        var mockedApplicationProvider = new Mock<IApplicationProvider>();
        var mockedRoleRepository = new Mock<IRoleRepository>();
        var target = new ApplicationsHttpTrigger(mockedLogger.Object,
                mockedConfiguration.Object,
                mockedUserRepository.Object,
                mockedRoleRepository.Object,
                mockedApplicationProvider.Object,
                mockedServiceProvider.Object
                );
        Assert.That(target,Is.Not.Null);
    }



}
