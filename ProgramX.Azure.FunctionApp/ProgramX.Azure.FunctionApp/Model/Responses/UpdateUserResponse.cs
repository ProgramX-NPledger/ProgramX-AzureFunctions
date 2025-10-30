using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class UpdateUserResponse : UpdateResponse
{
    [JsonPropertyName("userName")]
    public string Username { get; set; }
}