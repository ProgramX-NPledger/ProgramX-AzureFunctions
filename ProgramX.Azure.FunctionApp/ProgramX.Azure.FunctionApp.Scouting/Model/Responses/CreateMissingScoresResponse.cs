using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Scouting.Model.DTOs;

namespace ProgramX.Azure.FunctionApp.Scouting.Model.Responses;

public class CreateMissingScoresResponse
{
    [JsonPropertyName("successfullyCreatedScores")]
    public List<ScoutingScoreDto> SuccessfullyCreatedScores { get; set; }

    [JsonPropertyName("failedToCreateScores")]
    public List<ScoutingScoreDto> FailedToCreateScores { get; set; }

}