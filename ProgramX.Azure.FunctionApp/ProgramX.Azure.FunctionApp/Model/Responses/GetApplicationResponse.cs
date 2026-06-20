using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Model.Responses.Dtos;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetApplicationResponse
{
    /// <summary>
    /// The Application.
    /// </summary>
    [JsonPropertyName("application")]
    public ApplicationDto Application { get; set; }
    
}