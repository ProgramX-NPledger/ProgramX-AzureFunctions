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
    public async Task GetApplication_WithValidName_ShouldReturnApplication()
    {
        // Arrange
        const string appName = "TestApp";

        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.OK)
            .Build();

        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithIApplicationProvider(mockAppProvider =>
            {
                var mockApp = new Mock<IApplication>();
                mockApp.Setup(x => x.GetApplicationMetaData()).Returns(new ApplicationMetaData
                {
                    Name = appName,
                    FriendlyName = "Test App",
                    TargetUrl = "https://testapp.com",
                    RequiresRoleNames = ["admin"]
                });
                mockAppProvider.Setup(x => x.GetApplication(It.IsAny<string>())).Returns(mockApp.Object);
            })
            .Build();

        // Act
        var result = await applicationsHttpTrigger.GetApplication(testableHttpRequestData, appName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain(appName);
    }

    [Test]
    public async Task GetApplication_WithNonExistentName_ShouldReturnNotFound()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NotFound)
            .Build();

        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithIApplicationProvider(mockAppProvider =>
            {
                mockAppProvider.Setup(x => x.GetApplication(It.IsAny<string>())).Returns((IApplication?)null);
            })
            .Build();

        // Act
        var result = await applicationsHttpTrigger.GetApplication(testableHttpRequestData, "non-existent");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetApplication_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .Returns(HttpStatusCode.OK)
            .Build();

        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder().Build();

        // Act
        var result = await applicationsHttpTrigger.GetApplication(testableHttpRequestData, "some-app");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetApplication_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithInvalidAuthentication()
            .Returns(HttpStatusCode.OK)
            .Build();

        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder().Build();

        // Act
        var result = await applicationsHttpTrigger.GetApplication(testableHttpRequestData, "some-app");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetApplication_WithoutName_ShouldReturnAllApplications()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .Build();

        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithIApplicationProvider(mockAppProvider =>
            {
                var mockApp1 = new Mock<IApplication>();
                mockApp1.Setup(x => x.GetApplicationMetaData()).Returns(new ApplicationMetaData
                {
                    Name = "App1", FriendlyName = "Application 1", TargetUrl = "https://app1.com", RequiresRoleNames = []
                });
                var mockApp2 = new Mock<IApplication>();
                mockApp2.Setup(x => x.GetApplicationMetaData()).Returns(new ApplicationMetaData
                {
                    Name = "App2", FriendlyName = "Application 2", TargetUrl = "https://app2.com", RequiresRoleNames = []
                });
                mockAppProvider
                    .Setup(x => x.GetAllApplications(It.IsAny<GetAllApplicationsCriteria>()))
                    .Returns([mockApp1.Object, mockApp2.Object]);
            })
            .Build();

        // Act
        var result = await applicationsHttpTrigger.GetApplication(testableHttpRequestData, null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("App1");
        body.Should().Contain("App2");
    }

    [Test]
    public async Task GetApplication_WithContainsTextFilter_ShouldReturnFilteredApplications()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .WithAuthentication()
            .WithQuery(new NameValueCollection { { "containsText", "Admin" } })
            .Build();

        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithIApplicationProvider(mockAppProvider =>
            {
                var mockApp1 = new Mock<IApplication>();
                mockApp1.Setup(x => x.GetApplicationMetaData()).Returns(new ApplicationMetaData
                {
                    Name = "AdminApp", FriendlyName = "Admin Application", TargetUrl = "https://admin.com", RequiresRoleNames = []
                });
                var mockApp2 = new Mock<IApplication>();
                mockApp2.Setup(x => x.GetApplicationMetaData()).Returns(new ApplicationMetaData
                {
                    Name = "UserApp", FriendlyName = "User Application", TargetUrl = "https://user.com", RequiresRoleNames = []
                });
                mockAppProvider
                    .Setup(x => x.GetAllApplications(It.IsAny<GetAllApplicationsCriteria>()))
                    .Returns([mockApp1.Object, mockApp2.Object]);
            })
            .Build();

        // Act
        var result = await applicationsHttpTrigger.GetApplication(testableHttpRequestData, null);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("AdminApp");
        body.Should().NotContain("UserApp");
    }

    [Test]
    public async Task GetApplication_WithoutAuthentication_AndNoName_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestData = new TestableHttpRequestDataFactory().Create()
            .Returns(HttpStatusCode.OK)
            .Build();

        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder().Build();

        // Act
        var result = await applicationsHttpTrigger.GetApplication(testableHttpRequestData, null);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
