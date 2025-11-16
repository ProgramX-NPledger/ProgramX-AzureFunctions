using System.Net;
using FluentAssertions;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.ApplicationsHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("ApplicationsHttpTrigger")]
[Category("CreateApplication")]
[TestFixture]
public class ApplicationsHttpTriggerCreateApplicationTests 
{
   
    
    [Test]
    public async Task CreateApplication_WithValidRequest_ShouldReturnOkAndUserShouldBeCreated()
    {
        // Arrange
        const string applicationName = "test-application-123";

        var createApplicationRequest = new CreateApplicationRequest()
        {
            name = "test-application",
            description = "test application description",
            targetUrl = "https://test.example.com"
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(createApplicationRequest)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var applicationsHttpTrigger = new ApplicationsHttpTriggerBuilder()
            .WithIUserRepository(userRepository =>
            {
                var mockPagedResult = new Mock<IPagedResult<Application>>();
                mockPagedResult.SetupGet(x => x.Items).Returns(new List<Application>());
                
                userRepository.Setup(x =>x.GetApplicationsAsync(It.IsAny<GetApplicationsCriteria>(),It.IsAny<PagedCriteria>()))
                    .ReturnsAsync(mockPagedResult.Object);
            })
            .Build();
        
        // Act
        var result = await applicationsHttpTrigger.CreateApplication(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify the response contains user and applications
        var responseBody = await HttpBodyUtilities.GetStringFromHttpResponseDataBodyAsync(result);
        
        Assert.That(responseBody, Is.Not.Null);

        
    }
    
    
    
    
    [Test]
    public async Task CreateRole_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        const string roleName = "test-role-123";
        
        var createRoleRequest = new CreateRoleRequest()
        {
            name = "test-role",
            description = "test role description"
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithPayload(createRoleRequest)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .Build();
        
        // Act
        var result = await rolesHttpTrigger.CreateRole(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }
    
    
    [Test]
    public async Task CreateRole_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string roleName = "test-role-123";

        var createRoleRequest = new CreateRoleRequest()
        {
           name = roleName,
           description = "test role description"
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithInvalidAuthentication()
            .WithPayload(createRoleRequest)
            .Returns(HttpStatusCode.Unauthorized)
            .Build();
        
        var rolesHttpTrigger = new RolesHttpTriggerBuilder()
            .Build();
        
        // Act
        var result = await rolesHttpTrigger.CreateRole(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);;

    }
    
    
   
}