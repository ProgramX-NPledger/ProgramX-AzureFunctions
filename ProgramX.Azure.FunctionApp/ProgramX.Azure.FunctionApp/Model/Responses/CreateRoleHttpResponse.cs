using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ProgramX.Azure.FunctionApp.Model.Requests;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class CreateRoleHttpResponse : HttpResponseBase
{
    
    public Role Role { get; set; }
    
    public CreateRoleHttpResponse(HttpRequestData httpRequestData,CreateRoleRequest role)
    {
        using var hmac = new HMACSHA512();
        
        var newRole = new Role()
        {
            Name = role.name,
            Description = role.description
        };

        Role = newRole;
        
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.Created);
        HttpResponseData.Headers.Add("Location", new[] { $"{httpRequestData.Url}/role/{Role.Name}" });
        
        HttpResponseData.WriteAsJsonAsync(newRole);
    }
}