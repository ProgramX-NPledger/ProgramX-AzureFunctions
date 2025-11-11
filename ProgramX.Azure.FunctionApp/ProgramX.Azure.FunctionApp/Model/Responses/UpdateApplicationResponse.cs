using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class UpdateApplicationResponse : UpdateResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}