using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class BadRequestHttpResponse : HttpResponseBase
{
    public BadRequestHttpResponse(HttpRequestData httpRequestData, string errorMessage)
    {
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.BadRequest);
        HttpResponseData.WriteString(errorMessage);
    }
}