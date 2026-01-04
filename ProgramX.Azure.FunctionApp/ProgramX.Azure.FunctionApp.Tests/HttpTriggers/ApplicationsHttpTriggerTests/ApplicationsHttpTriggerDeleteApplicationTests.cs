using System.Net;
using FluentAssertions;
using Moq;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.ApplicationsHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("ApplicationsHttpTrigger")]
[Category("DeleteApplication")]
[TestFixture]
public class ApplicationsHttpTriggerDeleteApplicationTests
{
    [Test]
    public async Task DeleteApplication_WhenApplicationExists_ShouldDeleteSuccessfully()
    {
        // Arrange
        const string applicationName = "test-application-id";

        var existingApplication = new Application()
        {
            name = applicationName,
            metaDataDotNetAssembly = string.Empty,
            metaDataDotNetType = string.Empty
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithIUserRepository(mockUserRepository =>
            {
                mockUserRepository.Setup(x => x.GetApplicationByNameAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingApplication);
                mockUserRepository.Setup(x => x.DeleteApplicationByNameAsync(It.IsAny<string>()));
            })
            .Build();

        // Act
        var result = await applicationsHttpTrigger.DeleteApplication(testableHttpRequestData, applicationName);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteApplication_WhenApplicationDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .Returns(HttpStatusCode.NotFound)
            .Build();
        
        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
                .WithIUserRepository(mockUserRepository =>
                {
                    mockUserRepository.Setup(x => x.GetApplicationByNameAsync(It.IsAny<string>()))
                        .ReturnsAsync((Application)null!);
                })
                .Build();

        // Act
        var result = await applicationsHttpTrigger.DeleteApplication(testableHttpRequestData, "does not exist");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteApplication_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string applicationName = "test-application-id";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .Returns(HttpStatusCode.Unauthorized)
            .Build();        
        
        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .Build();

        // Act
        var result = await applicationsHttpTrigger.DeleteApplication(testableHttpRequestData, applicationName);

        // Assert
        result.Should().NotBeNull();
        // no header is added so it is a bad request
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    
    [Test]
    public async Task DeleteApplication_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string applicationName = "test-application-id";
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithInvalidAuthentication()
            .Returns(HttpStatusCode.Unauthorized)
            .Build();        
        
        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .Build();

        // Act
        var result = await applicationsHttpTrigger.DeleteApplication(testableHttpRequestData, applicationName);

        // Assert
        result.Should().NotBeNull();
        // no header is added so it is a bad request
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

}
