using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetRoleHttpResponse : HttpResponseBase
{
    private readonly Role _role;
    private readonly IEnumerable<Application> _flattenedApplications;
    private readonly IEnumerable<SecureUser> _allUsers;
    private readonly IEnumerable<UserInRole> _usersInRole;

    public GetRoleHttpResponse(HttpRequestData httpRequestData, 
        Role role, 
        IEnumerable<Application> flattenedApplications, 
        IEnumerable<SecureUser> allUsers, 
        IEnumerable<UserInRole> usersInRole)
    {
        _role = role;
        _flattenedApplications = flattenedApplications;
        _allUsers = allUsers;
        _usersInRole = usersInRole;
        HttpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);

    }

    public override async Task<HttpResponseBase> GetHttpResponseAsync()
    {
        await HttpResponseData.WriteAsJsonAsync(new
        {
            role = _role,
            applications = _flattenedApplications,
            _allUsers,
            _usersInRole
        });
        return this;
    }
}