using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Model.DTOs.Osm;

namespace ProgramX.Azure.FunctionApp.Model.DTOs;

public class GetScoutingScoresResponse
{
    [JsonPropertyName("items")]
    public List<ScoutingScoreDto> Items { get; set; }
}