using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class InvalidCredentialsOrUnauthorisedHttpResponse : LoginHttpResponse
{
    public InvalidCredentialsOrUnauthorisedHttpResponse(HttpRequestData httpRequestData)
    {
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
        HttpResponseData.WriteStringAsync("Invalid Credentials or Unauthorised");
    }
}