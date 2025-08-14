using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetApplicationsHttpResponse : HttpResponseBase
{
    private readonly IEnumerable<Application> _applications;

    public GetApplicationsHttpResponse(HttpRequestData httpRequestData, IEnumerable<Application> applications)
    {
        _applications = applications;
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
    }

    public override async Task<HttpResponseBase> GetHttpResponseAsync()
    {
        await HttpResponseData.WriteAsJsonAsync(_applications);
        return this;
    }
}