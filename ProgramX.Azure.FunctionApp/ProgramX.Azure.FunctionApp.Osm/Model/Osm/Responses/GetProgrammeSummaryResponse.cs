using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Osm.Model.Osm.Responses;

public class GetProgrammeSummaryResponse
{
    [JsonPropertyName("items")]
    public IEnumerable<Evening> Items { get; set; }
    
}