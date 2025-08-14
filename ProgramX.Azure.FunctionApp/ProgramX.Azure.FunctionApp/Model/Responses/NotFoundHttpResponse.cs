using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class NotFoundHttpResponse : HttpResponseBase
{
    private readonly string _type;

    public NotFoundHttpResponse(HttpRequestData httpRequestData, string type)
    {
        _type = type;
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.NotFound);
    }

    public override async Task<HttpResponseBase> GetHttpResponseAsync()
    {
        await HttpResponseData.WriteStringAsync($"{_type} not found.");
        return this;
    }
}