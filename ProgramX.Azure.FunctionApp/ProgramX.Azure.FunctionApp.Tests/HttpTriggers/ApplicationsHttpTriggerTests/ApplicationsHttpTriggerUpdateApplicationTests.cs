using System.Net;
using FluentAssertions;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.ApplicationsHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("ApplicationsHttpTrigger")]
[Category("UpdateApplication")]
[TestFixture]
public class ApplicationsHttpTriggerUpdateApplicationTests : TestBase
{
    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

    }

    
    [Test]
    public async Task UpdateApplication_WithValidId_ShouldReturnOkAndUserShouldBeUpdated()
    {
        // Arrange
        const string applicationName = "test-application-123";
        const string expectedDescription = "updated description";

        var existingApplication = new Application
        {
            name = "existingApplication",
            metaDataDotNetAssembly = string.Empty,
            metaDataDotNetType = string.Empty
        };
        var updateApplicationRequest = new UpdateApplicationRequest()
        {
            description = expectedDescription,
            name = applicationName,
            targetUrl = "https://updated.example.com"
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateApplicationRequest)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                var mockApplicationsResult = new Mock<IResult<Application>>();
                mockApplicationsResult.SetupGet(x => x.Items).Returns(new List<Application>()
                {
                    existingApplication
                });
                
                mockUserRepository.Setup(x => x.GetApplicationByNameAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingApplication);
                mockUserRepository.Setup(x =>
                    x.GetApplicationsAsync(It.IsAny<GetApplicationsCriteria>(), It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockApplicationsResult.Object);
                // mockUserRepository.Setup(x => x.GetRoleByNameAsync(It.IsAny<string>()))
                //     .ReturnsAsync(existingApplication);
                mockUserRepository.Setup(x => x.UpdateApplicationAsync(It.IsAny<string>(),It.IsAny<Application>()));
            })
            .Build();
        
        // Act
        var result = await applicationsHttpTrigger.UpdateApplication(testableHttpRequestData, applicationName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        
    }
    
    
    
    [Test]
    public async Task UpdateApplication_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        const string applicationName = "test-application-123";

        var updateApplication = new UpdateApplicationRequest()
        {
            name = applicationName,
            targetUrl = "https://updated.example.com",
            description = "updated description"
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(updateApplication)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await applicationsHttpTrigger.UpdateApplication(testableHttpRequestData, applicationName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);

    }
    
    [Test]
    public async Task UpdateApplication_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        const string applicationName = "test-application-123";
        
        var updateApplication = new UpdateApplicationRequest()
        {
            name = applicationName,
            targetUrl = "https://updated.example.com",
            description = "updated description"
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithPayload(updateApplication)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await applicationsHttpTrigger.UpdateApplication(testableHttpRequestData, applicationName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }
    
    
    [Test]
    public async Task UpdateApplication_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string applicationName = "test-application-123";

        var updateApplication = new UpdateApplicationRequest()
        {
            targetUrl = "https://updated.example.com",
            name = applicationName,
            description = "updated description"
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithInvalidAuthentication()
            .WithPayload(updateApplication)
            .Returns(HttpStatusCode.Unauthorized)
            .Build();

        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithConfiguration(Configuration)
            .Build();
        
        // Act
        var result = await applicationsHttpTrigger.UpdateApplication(testableHttpRequestData, applicationName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);;

    }
    
}