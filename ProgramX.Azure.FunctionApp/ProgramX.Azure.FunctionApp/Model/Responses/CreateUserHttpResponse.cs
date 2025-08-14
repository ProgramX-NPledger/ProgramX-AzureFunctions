using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ProgramX.Azure.FunctionApp.Model.Requests;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class CreateUserHttpResponse : HttpResponseBase
{
    
    public User User { get; set; }
    
    public CreateUserHttpResponse(HttpRequestData httpRequestData,CreateUserRequest user, IEnumerable<Role> roles)
    {
        using var hmac = new HMACSHA512();
        roles = roles.ToList(); // avoid multiple enumeration
        
        var newUser = new User()
        {
            id = Guid.NewGuid().ToString("N"),
            emailAddress = user.EmailAddress,
            userName = user.UserName,
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(user.Password)),
            passwordSalt = hmac.Key,
            roles = roles.Where(q=>user.AddToRoles.Select(r=>r.Name).Contains(q.Name))
                .Union(
                    user.AddToRoles
                        .Where(q=>!roles
                            .Select(r=>r.Name)
                            .Contains(q.Name)
                        )
                        .Select(q=>new Role()
                {
                    Name = q.Name,
                    Description = q.Description,
                    Applications = q.Applications
                }))
                .ToList()
        };
        
        User = newUser;
        
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.Created);
        HttpResponseData.Headers.Add("Location", new[] { $"{httpRequestData.Url}/user/{User.id}" });
        
        
    }

    public override async Task<HttpResponseBase> GetHttpResponseAsync()
    {
        
        await HttpResponseData.WriteAsJsonAsync(User);
        return this;
    }
}