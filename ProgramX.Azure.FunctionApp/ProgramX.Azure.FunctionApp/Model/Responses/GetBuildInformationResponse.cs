using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetBuildInformationResponse
{
    [JsonPropertyName( "gitCommitHash")]
    public string GitCommitHash { get; set; }
    
    [JsonPropertyName( "buildNumber")]
    public string BuildNumber { get; set; }
    
    [JsonPropertyName( "deployedAt")]
    public string DeployedAt { get; set; }
}