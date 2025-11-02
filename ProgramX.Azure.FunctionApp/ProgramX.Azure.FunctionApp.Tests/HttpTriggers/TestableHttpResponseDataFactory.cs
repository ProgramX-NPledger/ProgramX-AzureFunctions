namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;

/// <summary>
/// Factory to create <see cref="TestHttpRequestData"/> instances.
/// </summary>
public class TestableHttpResponseDataFactory
{
    /// <summary>
    /// Creates a starting-point for configuring and creating a <see cref="TestHttpResponseData"/> instance.
    /// </summary>
    /// <returns>A <see cref="TestableHttpResponseDataBuilder"/> which can be used to configure the creation of the <see cref="TestHttpRequestData"/> instance.</returns>
    public TestableHttpResponseDataBuilder Create()
    {
        return new TestableHttpResponseDataBuilder();
    }
}