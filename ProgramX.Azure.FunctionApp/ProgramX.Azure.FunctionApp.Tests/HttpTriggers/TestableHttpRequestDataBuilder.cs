using System.Collections.Specialized;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;

public class TestableHttpRequestDataBuilder
{
    private HttpStatusCode? _httpStatusCode=null;
    private NameValueCollection _query = new();
    private Uri _url = new("https://localhost");
    private IEnumerable<string> _roles = new List<string>();
    private bool? _useValidAuthorisation = null;
    
    public TestableHttpRequestDataBuilder Returns(HttpStatusCode httpStatusCode)
    {
        _httpStatusCode = httpStatusCode;
        return this;
    }

    public TestableHttpRequestDataBuilder WithAuthentication()
    {
        _useValidAuthorisation = true;
        return this;
    }
    
    public TestableHttpRequestDataBuilder WithInvalidAuthentication()
    {
        _useValidAuthorisation = false;
        return this;
    }

    public TestableHttpRequestDataBuilder GrantRoles(IEnumerable<string> roles)
    {
        _roles = roles;
        return this;
    }

    public TestableHttpRequestDataBuilder WithQuery(NameValueCollection query)
    {
        _query = query;
        return this;
    }
    
    public TestableHttpRequestDataBuilder WithUrl(Uri url)
    {
        _url = url;
        return this;
    }

    public HttpRequestData Build()
    {
        var mockFunctionContext = new Mock<FunctionContext>();
        
        var testHttpRequestData = new TestHttpRequestData(
            mockFunctionContext.Object, 
            _query, 
            _url,
            _httpStatusCode ?? HttpStatusCode.OK,
            _roles,
            _useValidAuthorisation);
        
        return testHttpRequestData;
    }
}