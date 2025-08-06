using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ProgramX.Azure.FunctionApp.Model.Requests;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class CreateUserHttpResponse : HttpResponseBase
{
    
    public User User { get; set; }
    
    public CreateUserHttpResponse(HttpRequestData httpRequestData,CreateUserRequest user)
    {
        var newUser = new User()
        {
            id = Guid.NewGuid().ToString("N"),
            emailAddress = user.emailAddress,
            userName = user.userName
        };

        User = newUser;
        
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.Created);
        HttpResponseData.Headers.Add("Location", new[] { $"{httpRequestData.Url}/user/{User.id}" });

        HttpResponseData.WriteAsJsonAsync(newUser);
    }
}