using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.DTOs.Osm.Response;

public class GetScoutingScoreItemsResponse
{
    [JsonPropertyName("items")]
    public List<ScoutingScoreItemDto> Items { get; set; }
}