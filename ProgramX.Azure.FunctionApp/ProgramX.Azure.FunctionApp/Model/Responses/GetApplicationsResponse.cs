using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Model.Responses.Dtos;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetApplicationsResponse
{
    [JsonPropertyName("applications")]
    public IEnumerable<ApplicationDto> Applications { get; set; }
}