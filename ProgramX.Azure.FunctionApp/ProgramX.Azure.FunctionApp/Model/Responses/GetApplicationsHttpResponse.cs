using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetApplicationsHttpResponse : HttpResponseBase
{
    public GetApplicationsHttpResponse(HttpRequestData httpRequestData, IEnumerable<Application> applications)
    {
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        HttpResponseData.WriteAsJsonAsync(applications);
    }
}