using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Tests.TestHelpers;

public static class HttpBodyUtilities
{
    public static async Task<string> GetResponseBodyAsync(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
    }
}