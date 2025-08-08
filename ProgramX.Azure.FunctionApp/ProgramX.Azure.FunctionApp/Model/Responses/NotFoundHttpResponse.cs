using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class NotFoundHttpResponse : HttpResponseBase
{
    public NotFoundHttpResponse(HttpRequestData httpRequestData, string type)
    {
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.NotFound);
        HttpResponseData.WriteStringAsync($"{type} not found.");
    }
}