using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetUsersHttpResponse : HttpResponseBase
{
    public GetUsersHttpResponse(HttpRequestData httpRequestData, IEnumerable<User> users, string? continuationToken)
    {
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        if (continuationToken != null)
        {
            HttpResponseData.Headers.Add("x-continuation-token", Uri.EscapeDataString(continuationToken));
        }
        HttpResponseData.WriteAsJsonAsync(users);
    }
}