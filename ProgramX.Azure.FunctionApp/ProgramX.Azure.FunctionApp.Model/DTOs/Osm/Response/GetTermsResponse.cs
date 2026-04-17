using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.DTOs.Osm.Response;

public class GetTermsResponse
{
    [JsonPropertyName("items")]
    public List<TermDto> Items { get; set; }
}