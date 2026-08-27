using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Scouting.Model.DTOs;

namespace ProgramX.Azure.FunctionApp.Scouting.Model.Responses;

public class GetScoutingScoresResponse
{
    [JsonPropertyName("items")]
    public List<ScoutingScoreDto> Items { get; set; }
}