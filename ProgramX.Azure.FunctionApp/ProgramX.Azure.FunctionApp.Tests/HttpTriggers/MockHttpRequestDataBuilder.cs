using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using System.Collections.Specialized;

namespace ProgramX.Azure.FunctionApp.Tests.TestData;

public class MockHttpRequestDataBuilder
{
    private readonly Mock<HttpRequestData> _mockHttpRequestData;
    private readonly NameValueCollection _query;

    public MockHttpRequestDataBuilder()
    {
        _mockHttpRequestData = new Mock<HttpRequestData>();
        _query = new NameValueCollection();
        
        _mockHttpRequestData.Setup(x => x.Query).Returns(_query);
        _mockHttpRequestData.Setup(x => x.Url).Returns(new Uri("https://localhost:7071/api/user"));
    }

    public MockHttpRequestDataBuilder WithUrl(string url)
    {
        _mockHttpRequestData.Setup(x => x.Url).Returns(new Uri(url));
        return this;
    }

    public MockHttpRequestDataBuilder WithQueryParameter(string key, string value)
    {
        _query[key] = value;
        return this;
    }

    public MockHttpRequestDataBuilder WithContainsText(string containsText)
    {
        _query["containsText"] = Uri.EscapeDataString(containsText);
        return this;
    }

    public MockHttpRequestDataBuilder WithRoles(params string[] roles)
    {
        _query["withRoles"] = Uri.EscapeDataString(string.Join(",", roles));
        return this;
    }

    public MockHttpRequestDataBuilder WithApplications(params string[] applications)
    {
        _query["hasAccessToApplications"] = Uri.EscapeDataString(string.Join(",", applications));
        return this;
    }

    public MockHttpRequestDataBuilder WithPaging(int offset, int itemsPerPage)
    {
        _query["offset"] = offset.ToString();
        _query["itemsPerPage"] = itemsPerPage.ToString();
        return this;
    }

    public MockHttpRequestDataBuilder WithContinuationToken(string token)
    {
        _query["continuationToken"] = Uri.EscapeDataString(token);
        return this;
    }

    public Mock<HttpRequestData> Build() => _mockHttpRequestData;

    public static implicit operator Mock<HttpRequestData>(MockHttpRequestDataBuilder builder) => builder.Build();
}
