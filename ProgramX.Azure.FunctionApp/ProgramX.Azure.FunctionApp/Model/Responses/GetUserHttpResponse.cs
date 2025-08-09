using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetUserHttpResponse : HttpResponseBase
{
    public GetUserHttpResponse(HttpRequestData httpRequestData, SecureUser user, IEnumerable<Application> flattenedApplications)
    {
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        HttpResponseData.WriteAsJsonAsync(new
        {
            user,
            applications = flattenedApplications,
            profilePhotoBase64 = string.Empty
        });
    }
}