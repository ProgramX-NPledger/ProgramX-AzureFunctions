using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class UpdateRoleResponse : UpdateResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}