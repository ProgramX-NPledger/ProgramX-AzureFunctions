using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Model.Responses.Dtos;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetUserResponse
{
    [JsonPropertyName("user")]
    public UserDto User { get; set; }
}