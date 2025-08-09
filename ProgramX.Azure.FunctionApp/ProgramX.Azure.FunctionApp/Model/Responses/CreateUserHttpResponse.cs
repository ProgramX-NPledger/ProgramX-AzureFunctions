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
            Id = Guid.NewGuid().ToString("N"),
            EmailAddress = user.emailAddress,
            UserName = user.userName,
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(user.password)),
            PasswordSalt = hmac.Key
        };

        User = newUser;
        
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.Created);
        HttpResponseData.Headers.Add("Location", new[] { $"{httpRequestData.Url}/user/{User.Id}" });

        // redact the password
        newUser.PasswordHash = new byte[0];
        newUser.PasswordSalt = new byte[0];
        
        HttpResponseData.WriteAsJsonAsync(newUser);
    }
}