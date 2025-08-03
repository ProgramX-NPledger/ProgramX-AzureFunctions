using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class LoginSuccessHttpResponse : LoginHttpResponse
{
    public LoginSuccessHttpResponse(HttpRequestData httpRequestData, string token)
    {
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        HttpResponseData.WriteAsJsonAsync(new
        {
            token = token
        });
    }
}