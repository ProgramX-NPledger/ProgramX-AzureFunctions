using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Moq;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using EmailMessage = Azure.Communication.Email.EmailMessage;

namespace ProgramX.Azure.FunctionApp.Tests.Helpers;

[TestFixture]
public class AzureCommunicationsServicesEmailSenderTests
{
    [Test]
    public void Ctor_WithInvalidConfiguration_ShouldThrow()
    {
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(x => x["AzureCommunicationsServices:ConnectionString"]).Returns("");
        
        Assert.Throws<ArgumentException>(() => _ = new AzureCommunicationsServicesEmailSender(mockConfiguration.Object));
    }
    
    [Test]
    public void Ctor_WithValidConfiguration_ShouldSucceed()
    {
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns("endpoint=https://cs-programx.uk.communication.azure.com/;accesskey=4hZdLw70VwtSoe2EbKFVA");
        
        var target = new AzureCommunicationsServicesEmailSender(mockConfiguration.Object);
        Assert.That(target, Is.Not.Null);
    }
    
    [Test]
    public void Ctor_WithEmailClient_ShouldSucceed()
    {
        var mockEmailClient = new Mock<EmailClient>();
        
        var target = new AzureCommunicationsServicesEmailSender(mockEmailClient.Object);
        Assert.That(target, Is.Not.Null);
    }

    // no tests for SendEmailAsync as it is a wrapper around EmailClient.SendAsync which has a non-settable Status output and
    // cannot be mocked

}