using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ProgramX.Azure.FunctionApp.Model.Requests;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class CreateRoleHttpResponse : HttpResponseBase
{
    private readonly HttpRequestData _httpRequestData;
    private readonly CreateRoleRequest _role;

    public Role Role { get; set; }

    public CreateRoleHttpResponse(HttpRequestData httpRequestData, CreateRoleRequest role)
    {
        _httpRequestData = httpRequestData;
        _role = role;
        using var hmac = new HMACSHA512();

        var newRole = new Role()
        {
            Name = role.name,
            Description = role.description
        };

        Role = newRole;

        HttpResponseData = _httpRequestData.CreateResponse(System.Net.HttpStatusCode.Created);
    }
    
    public override async Task<HttpResponseBase> GetHttpResponseAsync()
    {
        HttpResponseData.Headers.Add("Location", new[] { $"{_httpRequestData.Url}/role/{Role.Name}" });
        await HttpResponseData.WriteAsJsonAsync(Role);
        return this;
    }
}