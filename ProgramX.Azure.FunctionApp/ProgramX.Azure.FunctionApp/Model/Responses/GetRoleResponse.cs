using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Model.Responses.Dtos;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetRoleResponse
{
    [JsonPropertyName("role")]
    public RoleDto Role { get; set; }
    [JsonPropertyName("usersInRole")]
    public IEnumerable<string> UsersInRole { get; set; }
    [JsonPropertyName("applicationsWithRole")]
    public IEnumerable<string> ApplicationsWithRole { get; set; }
}