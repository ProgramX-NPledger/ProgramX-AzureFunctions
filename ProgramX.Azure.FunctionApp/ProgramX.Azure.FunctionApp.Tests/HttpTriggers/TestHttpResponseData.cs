using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;

public sealed class TestHttpResponseData : HttpResponseData
{
    public TestHttpResponseData(FunctionContext functionContext, 
        HttpStatusCode httpStatusCode) : base(functionContext)
    {
        StatusCode = httpStatusCode;
        Headers = new HttpHeadersCollection();
        Body = new MemoryStream();
        Cookies = new EmptyHttpCookies();
    }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; }
    public override Stream Body { get; set; }
    public override HttpCookies Cookies { get; }
    
    
}