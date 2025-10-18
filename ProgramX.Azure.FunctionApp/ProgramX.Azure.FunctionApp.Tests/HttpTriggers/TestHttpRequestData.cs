using System.Collections.Specialized;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;


public class TestHttpRequestData : HttpRequestData
{
    private NameValueCollection _query = new();
    private Uri _url = new("https://localhost");
    
    public override Stream Body { get; }
    public override HttpHeadersCollection Headers { get; }
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; }
    public override Uri Url => _url;
    public override IEnumerable<ClaimsIdentity> Identities { get; }
    public override string Method { get; }
    public override HttpResponseData CreateResponse() => throw new NotImplementedException();

    public TestHttpRequestData() : base(new TestFunctionContext())
    {
        Body = new MemoryStream();
        Headers = new HttpHeadersCollection();
        Cookies = new List<IHttpCookie>();
        Identities = new List<ClaimsIdentity>();
        Method = "GET";
    }

    public void SetQuery(NameValueCollection query) => _query = query;
    public void SetUrl(Uri url) => _url = url;
    
    public NameValueCollection Query => _query;
}

