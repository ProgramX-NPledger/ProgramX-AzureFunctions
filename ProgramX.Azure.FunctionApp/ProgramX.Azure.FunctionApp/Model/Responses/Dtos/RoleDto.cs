using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses.Dtos;

public class RoleDto
{
    [JsonPropertyName("roleName")]
    public required string RoleName { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}