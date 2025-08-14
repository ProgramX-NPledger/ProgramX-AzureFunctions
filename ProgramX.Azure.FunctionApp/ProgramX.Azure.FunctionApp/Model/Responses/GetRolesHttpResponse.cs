using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetRolesHttpResponse : HttpResponseBase
{
    private readonly IEnumerable<Role> _roles;

    public GetRolesHttpResponse(HttpRequestData httpRequestData, IEnumerable<Role> roles, string? continuationToken)
    {
        _roles = roles;
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        if (continuationToken != null)
        {
            HttpResponseData.Headers.Add("x-continuation-token", Uri.EscapeDataString(continuationToken));
        }
        
    }

    public override async Task<HttpResponseBase> GetHttpResponseAsync()
    {
        await HttpResponseData.WriteAsJsonAsync(_roles);
        return this;
    }
}