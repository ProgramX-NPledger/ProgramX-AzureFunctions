using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetHealthCheckResponse
{
    [JsonPropertyName("timeStamp")]
    public DateTime TimeStamp {
        get;
        set;
    }

    [JsonPropertyName("services")] public IList<HealthCheckService> Services { get; set; } = [];

}