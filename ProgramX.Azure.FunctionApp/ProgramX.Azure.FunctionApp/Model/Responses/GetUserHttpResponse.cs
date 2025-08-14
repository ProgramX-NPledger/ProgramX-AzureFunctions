using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetUserHttpResponse : HttpResponseBase
{
    private readonly SecureUser _user;
    private readonly IEnumerable<Application> _flattenedApplications;

    public GetUserHttpResponse(HttpRequestData httpRequestData, SecureUser user, IEnumerable<Application> flattenedApplications)
    {
        _user = user;
        _flattenedApplications = flattenedApplications;
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
    }

    public override async Task<HttpResponseBase> GetHttpResponseAsync()
    {
        await HttpResponseData.WriteAsJsonAsync(new
        {
            _user,
            applications = _flattenedApplications,
            profilePhotoBase64 = string.Empty
        });
        return this;
    }
}