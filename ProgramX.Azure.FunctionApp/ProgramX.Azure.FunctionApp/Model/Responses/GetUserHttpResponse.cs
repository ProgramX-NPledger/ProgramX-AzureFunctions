using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetUserHttpResponse : HttpResponseBase
{
    public GetUserHttpResponse(HttpRequestData httpRequestData, SecureUser user, IEnumerable<Application> applications, IEnumerable<string> roles)
    {
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        HttpResponseData.WriteAsJsonAsync(new
        {
            user,
            applications,
            profilePhotoBase64 = string.Empty,
            roles
        });
    }
}