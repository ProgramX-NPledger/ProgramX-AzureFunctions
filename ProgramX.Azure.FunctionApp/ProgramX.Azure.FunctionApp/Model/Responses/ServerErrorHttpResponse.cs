using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class ServerErrorHttpResponse : HttpResponseBase
{
    public ServerErrorHttpResponse(HttpRequestData httpRequestData, Exception exception)
    {
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.BadRequest);
        HttpResponseData.WriteString(exception.Message);
    }
}