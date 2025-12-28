using System.Net;
using FluentAssertions;
using Moq;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("HealthCheckHttpTrigger")]
[Category("GetHealthCheck")]
[TestFixture]
public class HealthCheckHttpTriggerTests
{
    [Test]
    public async Task GetHealthCheck_WithNoParameters_ShouldReturnOkWithDefaultResponse()
    {
        // Arrange
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var healthCheckHttpTrigger = new HealthCheckHttpTriggerBuilder()
            .Build();
        
        // Act
        var result = await healthCheckHttpTrigger.GetServiceHealthCheck(testableHttpRequestData, null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the response contains user and applications
        var responseBody = await TestHelpers.HttpBodyUtilities.GetResponseBodyAsync(result);
        responseBody.Should().Contain("azure-functions");
    }
    
    [Test]
    public async Task GetHealthCheck_Within20Seconds_ShouldReturnTooManyRequests()
    {
        // Arrange
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var healthCheckHttpTrigger = new HealthCheckHttpTriggerBuilder()
            .WithSingletonMutext(mockSingletonMutex =>
            {
                mockSingletonMutex.Setup(x => x.IsRequestWithinSecondsOfMostRecentRequestOfSameType(It.IsAny<string>())).Returns(true);
                mockSingletonMutex.SetupGet(x => x.SecondsTimeout).Returns(20);
            })
            .Build();
        
        // Act
        var result = await healthCheckHttpTrigger.GetServiceHealthCheck(testableHttpRequestData, null);
        result = await healthCheckHttpTrigger.GetServiceHealthCheck(testableHttpRequestData, null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
    
    
    [Test]
    public async Task GetHealthCheck_After2Seconds_ShouldReturnTooManyRequests()
    {
        // Arrange
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var healthCheckHttpTrigger = new HealthCheckHttpTriggerBuilder()
            .WithSingletonMutext(mockSingletonMutex =>
            {
                mockSingletonMutex.SetupGet(x => x.SecondsTimeout).Returns(2);
            })
            .Build();
        
        // Act
        var result = await healthCheckHttpTrigger.GetServiceHealthCheck(testableHttpRequestData, null);
        Thread.Sleep(3000);
        result = await healthCheckHttpTrigger.GetServiceHealthCheck(testableHttpRequestData, null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    
    
    [Test]
    public async Task GetHealthCheck_WithinKnownType_ShouldReturnOK()
    {
        // Arrange
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var healthCheckHttpTrigger = new HealthCheckHttpTriggerBuilder()
            .Build();
        
        var knownType = "test";
        // Act
        var result = await healthCheckHttpTrigger.GetServiceHealthCheck(testableHttpRequestData, knownType);
        
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify the response contains user and applications
        var responseBody = await TestHelpers.HttpBodyUtilities.GetResponseBodyAsync(result);
        responseBody.Should().Contain(knownType);
    }
    
    [Test]
    public async Task GetHealthCheck_WithinUnknownType_ShouldReturnBadRequest()
    {
        // Arrange
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .Returns(HttpStatusCode.NoContent)
            .Build();
        
        var healthCheckHttpTrigger = new HealthCheckHttpTriggerBuilder()
            .Build();
        
        // Act
        var result = await healthCheckHttpTrigger.GetServiceHealthCheck(testableHttpRequestData, "type");
        
        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
    }
}