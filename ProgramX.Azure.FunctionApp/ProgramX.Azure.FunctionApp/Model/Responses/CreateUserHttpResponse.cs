using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ProgramX.Azure.FunctionApp.Model.Requests;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class CreateUserHttpResponse : HttpResponseBase
{
    
    public User User { get; set; }
    
    public CreateUserHttpResponse(HttpRequestData httpRequestData,CreateUserRequest user)
    {
        using var hmac = new HMACSHA512();
        
        var newUser = new User()
        {
            id = Guid.NewGuid().ToString("N"),
            emailAddress = user.emailAddress,
            userName = user.userName,
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(user.password)),
            passwordSalt = hmac.Key
        };

        User = newUser;
        
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.Created);
        HttpResponseData.Headers.Add("Location", new[] { $"{httpRequestData.Url}/user/{User.id}" });

        HttpResponseData.WriteAsJsonAsync(newUser);
    }
}