using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetUsersHttpResponse : HttpResponseBase
{
    private readonly IEnumerable<SecureUser> _users;

    public GetUsersHttpResponse(HttpRequestData httpRequestData, IEnumerable<SecureUser> users, string? continuationToken)
    {
        _users = users;
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        if (continuationToken != null)
        {
            HttpResponseData.Headers.Add("x-continuation-token", Uri.EscapeDataString(continuationToken));
        }
        
    }

    public override async Task<HttpResponseBase> GetHttpResponseAsync()
    {
        await HttpResponseData.WriteAsJsonAsync(_users);
        return this;
    }
}