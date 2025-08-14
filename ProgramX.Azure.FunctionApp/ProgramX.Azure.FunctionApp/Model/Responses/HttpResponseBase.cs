using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public abstract class HttpResponseBase
{
    public HttpResponseData HttpResponseData { get; protected set; }
    public abstract Task<HttpResponseBase> GetHttpResponseAsync();
}