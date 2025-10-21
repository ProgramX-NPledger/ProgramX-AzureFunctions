using System.Collections.Specialized;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;

public class TestableHttpRequestDataFactory
{
    
    public TestableHttpRequestDataBuilder Create()
    {
        return new TestableHttpRequestDataBuilder();
    }
}