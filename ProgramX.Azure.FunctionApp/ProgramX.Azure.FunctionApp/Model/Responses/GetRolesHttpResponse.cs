using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetRolesHttpResponse : HttpResponseBase
{
    public GetRolesHttpResponse(HttpRequestData httpRequestData, IEnumerable<Role> roles, string? continuationToken)
    {
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        if (continuationToken != null)
        {
            HttpResponseData.Headers.Add("x-continuation-token", Uri.EscapeDataString(continuationToken));
        }
        HttpResponseData.WriteAsJsonAsync(roles);
    }
}