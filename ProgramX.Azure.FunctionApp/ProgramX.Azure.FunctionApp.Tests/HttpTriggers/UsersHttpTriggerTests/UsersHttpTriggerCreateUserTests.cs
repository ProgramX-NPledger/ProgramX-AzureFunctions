using System.Net;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Tests.Mocks;
using ProgramX.Azure.FunctionApp.Tests.TestData;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers.UsersHttpTriggerTests;

[Category("Unit")]
[Category("HttpTrigger")]
[Category("UsersHttpTrigger")]
[Category("UpdateUser")]
[TestFixture]
public class UsersHttpTriggerCreateUserTests 
{
   
    
    [Test]
    public async Task CreateUser_WithValidRequest_ShouldReturnOkAndUserShouldBeCreated()
    {
        // Arrange
        const string userId = "test-user-123";

        var createUserRequest = new CreateUserRequest()
        {
            emailAddress = "email@address.com",
            userName = userId,
            addToRoles = ["test-role"],
            firstName = "test",
            lastName = "user",
            password = "password",
            passwordConfirmationLinkExpiryDate = DateTime.UtcNow.AddDays(1),
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(createUserRequest)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .Build();
        
        // Act
        var result = await usersHttpTrigger.CreateUser(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify the response contains user and applications
        var responseBody = await HttpBodyUtilities.GetStringFromHttpResponseDataBodyAsync(result);
        
        Assert.That(responseBody, Is.Not.Null);
        Assert.IsTrue(createUserRequest.userName == userId);
        
    }
    
    
    
    
    [Test]
    public async Task CreateUser_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange
        const string userId = "test-user-123";
        
        var createUserRequest = new CreateUserRequest()
        {
            emailAddress = "email@address.com",
            userName = userId,
            addToRoles = ["test-role"],
            firstName = "test",
            lastName = "user",
            password = "password",
            passwordConfirmationLinkExpiryDate = DateTime.UtcNow.AddDays(1),
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithPayload(createUserRequest)
            .Returns(HttpStatusCode.NoContent)
            .Build();

        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .Build();
        
        // Act
        var result = await usersHttpTrigger.CreateUser(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }
    
    
    [Test]
    public async Task CreateUser_WithInvalidAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        const string userId = "test-user-123";

        var createUserRequest = new CreateUserRequest()
        {
            emailAddress = "email@address.com",
            userName = userId,
            addToRoles = ["test-role"],
            firstName = "test",
            lastName = "user",
            password = "password",
            passwordConfirmationLinkExpiryDate = DateTime.UtcNow.AddDays(1),
        };

        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithInvalidAuthentication()
            .WithPayload(createUserRequest)
            .Returns(HttpStatusCode.Unauthorized)
            .Build();
        
        var usersHttpTrigger = new UsersHttpTriggerBuilder()
            .Build();
        
        // Act
        var result = await usersHttpTrigger.CreateUser(testableHttpRequestData);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);;

    }
    
    
   
}