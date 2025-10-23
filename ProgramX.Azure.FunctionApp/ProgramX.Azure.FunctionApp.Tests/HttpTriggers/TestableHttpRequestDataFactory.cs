namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;

/// <summary>
/// Factory to create <see cref="TestHttpRequestData"/> instances.
/// </summary>
public class TestableHttpRequestDataFactory
{
    /// <summary>
    /// Creates a starting-point for configuring and creating a <see cref="TestHttpRequestData"/> instance.
    /// </summary>
    /// <returns>A <see cref="TestableHttpRequestDataBuilder"/> which can be used to configure the creation of the <see cref="TestHttpRequestData"/> instance.</returns>
    public TestableHttpRequestDataBuilder Create()
    {
        return new TestableHttpRequestDataBuilder();
    }
}