using System.Net;
using System.Text.Json;
using Azure;
using Azure.Communication.Email;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Tests.HttpTriggers;
using EmailMessage = Azure.Communication.Email.EmailMessage;

namespace ProgramX.Azure.FunctionApp.Tests.Helpers;

[TestFixture]
public class HttpBodyUtilitiesTests
{
    [Test]
    public async Task GetStringFromHttpRequestDataBodyAsync_WithUnseekableBody_ShouldSucceed()
    {
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithBody("ABC")
            .Returns(HttpStatusCode.OK)
            .Build();
        
        var result = await HttpBodyUtilities.GetStringFromHttpRequestDataBodyAsync(testableHttpRequestData);
        
        Assert.That(result, Is.EqualTo("ABC"));
    }
    
    [Test]
    public async Task GetStringFromHttpResponseDataBodyAsync_WithUnseekableBody_ShouldSucceed()
    {
        var testableHttpResponseDataFactory = new TestableHttpResponseDataFactory();
        var testableHttpResponseData = testableHttpResponseDataFactory.Create()
            .Returns(HttpStatusCode.OK)
            .Build();
        
        var result = await HttpBodyUtilities.GetStringFromHttpResponseDataBodyAsync(testableHttpResponseData);
        
        Assert.That(result, Is.EqualTo("ABC"));
    }

    [Test]
    public async Task GetDeserializedJsonFromHttpRequestDataBodyAsync_WithSerializableObject_ShouldSucceed()
    {
        var serializedObject = new Role()
        {
            name = "test"
        };
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithPayload(serializedObject)
            .Returns(HttpStatusCode.OK)
            .Build();
        
        var result = await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<Role>(testableHttpRequestData);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.name, Is.EqualTo(serializedObject.name));
    }
    
    [Test]
    public async Task GetDeserializedJsonFromHttpRequestDataBodyAsync_WithUnserializableObjectAndNoThrowsErrorOption_ShouldThrow()
    {
        dynamic unserializableObject = new System.Dynamic.ExpandoObject();
        unserializableObject.Name = "Alice";
        unserializableObject.Age = 30;
        
        var testableHttpRequestDataFactory = new TestableHttpRequestDataFactory();
        var testableHttpRequestData = testableHttpRequestDataFactory.Create()
            .WithAuthentication()
            .WithBody("{invalid json}")
            .Returns(HttpStatusCode.OK)
            .Build();
        
        Assert.ThrowsAsync<JsonException>(async () => await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<Role>(testableHttpRequestData));
    }


    

}