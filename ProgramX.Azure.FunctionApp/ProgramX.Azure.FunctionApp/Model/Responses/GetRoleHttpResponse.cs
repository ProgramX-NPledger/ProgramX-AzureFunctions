using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetRoleHttpResponse : HttpResponseBase
{
    public GetRoleHttpResponse(HttpRequestData httpRequestData, 
        Role role, 
        IEnumerable<Application> flattenedApplications, 
        IEnumerable<SecureUser> allUsers, 
        IEnumerable<UserInRole> usersInRole)
    {
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        HttpResponseData.WriteAsJsonAsync(new
        {
            role = role,
            applications = flattenedApplications,
            allUsers,
            usersInRole
        });
    }
}