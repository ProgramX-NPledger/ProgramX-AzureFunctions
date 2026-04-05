using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class FixApplicationByHealthCheckResponse
{
    [JsonPropertyName("items")]
    public required IEnumerable<FixApplicationHealthCheckResultItemResult> Items { get; set; } = [];

}