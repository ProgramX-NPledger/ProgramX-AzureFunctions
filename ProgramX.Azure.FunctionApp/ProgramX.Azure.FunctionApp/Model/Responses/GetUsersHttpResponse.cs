using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetUsersHttpResponse : HttpResponseBase
{
    public GetUsersHttpResponse(HttpRequestData httpRequestData, IEnumerable<User> users)
    {
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        HttpResponseData.WriteAsJsonAsync(users);
    }
}