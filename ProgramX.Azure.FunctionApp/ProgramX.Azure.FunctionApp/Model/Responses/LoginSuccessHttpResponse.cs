using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class LoginSuccessHttpResponse : LoginHttpResponse
{
    private readonly string _jwtToken;

    public LoginSuccessHttpResponse(HttpRequestData httpRequestData, string jwtToken)
    {
        _jwtToken = jwtToken;
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
    }

    public async override Task<HttpResponseBase> GetHttpResponseAsync()
    {
        await HttpResponseData.WriteAsJsonAsync(new
        {
            token = _jwtToken,
        });
        return this;
    }
}