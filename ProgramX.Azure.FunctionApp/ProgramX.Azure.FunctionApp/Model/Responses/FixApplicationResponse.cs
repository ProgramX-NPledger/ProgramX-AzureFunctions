using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class FixApplicationResponse
{
    [JsonPropertyName("items")]
    public required IEnumerable<FixApplicationByHealthCheckResult> Items { get; set; } = [];

}