using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ProgramX.Azure.FunctionApp.Model.Requests;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class UpdateUserHttpResponse : HttpResponseBase
{
    
    //public User User { get; set; }
    
    public UpdateUserHttpResponse(HttpRequestData httpRequestData, UpdateUserRequest user)
    {
        //roles = roles.ToList(); // avoid multiple enumeration
        
        // var updatedUser = new SecureUser()
        // {
        //     id = Guid.NewGuid().ToString("N"),
        //     emailAddress = user.emailAddress,
        //     userName = user.userName,
        //     roles = roles.Where(q=>user.AddToRoles.Select(r=>r.Name).Contains(q.Name))
        //         .Union(
        //             user.AddToRoles
        //                 .Where(q=>!roles
        //                     .Select(r=>r.Name)
        //                     .Contains(q.Name)
        //                 )
        //                 .Select(q=>new Role()
        //         {
        //             Name = q.Name,
        //             Description = q.Description,
        //             Applications = q.Applications
        //         }))
        //         .ToList()
        // };
        
       // User = user;
        
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        //HttpResponseData.Headers.Add("Location", new[] { $"{httpRequestData.Url}/user/{user.}" });
        
        // HttpResponseData.WriteAsJsonAsync(new SecureUser()
        // {
        //     id = User.id,
        //     emailAddress = User.emailAddress,
        //     userName = User.userName,
        //     roles = User.roles
        // });
    }
}