using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Scouting.Model.DTOs;

namespace ProgramX.Azure.FunctionApp.Scouting.Model.Responses;

public class GetScoresDefinedStatusResponse
{
    [JsonPropertyName("hasAllScoresDefined")]
    public bool HasAllScoresDefined { get; set; }
    
    [JsonPropertyName("missingScores")]
    public List<ScoutingScoreDto> MissingScores { get; set; }
}