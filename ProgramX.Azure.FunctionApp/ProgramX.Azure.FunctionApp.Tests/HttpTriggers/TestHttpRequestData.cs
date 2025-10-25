using System.Collections.Specialized;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;

/// <summary>
/// Inherits and overrides elements of <see cref="HttpRequestData"/> to allow for testing
/// through the creation of a <see cref="TestHttpRequestData"/> instance.
/// </summary>
public class TestHttpRequestData : HttpRequestData
{
    public HttpStatusCode HttpStatusCode { get; }
    private Uri _url = new("https://localhost");
    private readonly object? _payload;
    private readonly JwtTokenIssuer _jwtTokenIssuer = new JwtTokenIssuer(null);
    
    public override Stream Body { get; }
    public override HttpHeadersCollection Headers { get; }
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; }
    public override Uri Url => _url;
    public override IEnumerable<ClaimsIdentity> Identities { get; }
    public override string Method { get; }
    private readonly IConfiguration _configuration;

    public TestHttpRequestData() : base(new TestFunctionContext())
    {
        Body = new MemoryStream();
        Headers = new HttpHeadersCollection();
        Cookies = new List<IHttpCookie>();
        Identities = new List<ClaimsIdentity>();
        Method = "GET";
        _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.test.json").Build();
    }

    public TestHttpRequestData(FunctionContext functionContext, 
        NameValueCollection mockQuery, 
        Uri uri,
        HttpStatusCode httpStatusCode = HttpStatusCode.OK,
        IEnumerable<string>? testWithRoles = null,
        bool? useAuthorisation = true,
        object? payload = null) : base(functionContext)
    {
        HttpStatusCode = httpStatusCode;
        _url = uri;
        _payload = payload;
        CopyIntoQueryString(mockQuery);
        Body = new MemoryStream();
        _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.test.json").Build();
        Headers = new HttpHeadersCollection();
        if (useAuthorisation.HasValue && useAuthorisation.Value)
        {
            Headers.Add(AuthorisedHttpTriggerBase.AuthenticationHeaderName, $"Bearer {CreateJwtTokenForTesting(testWithRoles ?? [])}");
        }
        if (useAuthorisation.HasValue && !useAuthorisation.Value)
        {
            Headers.Add(AuthorisedHttpTriggerBase.AuthenticationHeaderName, $"Bearer InvalidToken");
        }

        Cookies = new List<IHttpCookie>();
        Identities = new List<ClaimsIdentity>();
        Method = "GET";
    }

    private void CopyIntoQueryString(NameValueCollection mockQuery)
    {
        foreach (string key in mockQuery.AllKeys)
        {
            Query.Add(key, mockQuery[key]);
        }
    }

    private string CreateJwtTokenForTesting(IEnumerable<string> testWithRoles)
    {
        // no need to check the password hash, just create a token
        
        string token = _jwtTokenIssuer.IssueTokenForUser(new Credentials()
        {
            UserName = "test-user",
            Password = ""
        },testWithRoles,_configuration["JwtKey"]);
        return token;
    }

    public override HttpResponseData CreateResponse()
    {
        if (_payload == null)
        {
            var serializedPayload = JsonSerializer.Serialize(_payload);
            MemoryStream stream = new();
            stream.Write(Encoding.ASCII.GetBytes(serializedPayload), 0, serializedPayload.Length);
            stream.Position = 0;
            return new TestHttpResponseData(this.FunctionContext, HttpStatusCode)
            {
                Body = stream
            };
        }
        else
        {
            return new TestHttpResponseData(this.FunctionContext,HttpStatusCode);
        }
    }
    
    
    public void SetQuery(NameValueCollection query) => CopyIntoQueryString(query);
    public void SetUrl(Uri url) => _url = url;
    
}

