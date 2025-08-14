using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class ServerErrorHttpResponse : HttpResponseBase
{
    private readonly string _errorMessage;

    public ServerErrorHttpResponse(HttpRequestData httpRequestData, Exception exception)
    {
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.BadRequest);
        _errorMessage = exception.Message;
    }
    
    public ServerErrorHttpResponse(HttpRequestData httpRequestData, string errorMessage)
    {
        _errorMessage = errorMessage;
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.BadRequest);
        HttpResponseData.WriteString(errorMessage);
    }

    public override async Task<HttpResponseBase> GetHttpResponseAsync()
    {
        await HttpResponseData.WriteStringAsync(_errorMessage);
        return this;
    }
}