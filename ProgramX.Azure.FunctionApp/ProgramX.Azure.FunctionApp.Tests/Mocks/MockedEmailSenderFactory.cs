using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Tests.Mocks;

public class MockedEmailSenderFactory
{
   
    
    public MockedEmailSenderFactory()
    {
        
    }
    
    
    /// <summary>
    /// Creates the mock Email Sender.
    /// </summary>
    /// <returns>A mocked Email Sender.</returns>
    public Mock<IEmailSender> Create()
    {
        var mockedEmailSender = CreateEmailSender();
        return mockedEmailSender;
    }

    private Mock<IEmailSender> CreateEmailSender()
    {
        var mockEmailSender = new Mock<IEmailSender>();

        mockEmailSender.Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>()));
        
        return mockEmailSender;
    }

    
}