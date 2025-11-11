using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetHealthCheckResponse
{
    [JsonPropertyName("isAuthenticated")]
    public bool IsAuthenticated => false;

    [JsonPropertyName("healthCheckItems")]
    public IList<HealthCheckItem> HealthCheckItems { get; set; } = new List<HealthCheckItem>()
    {
        new HealthCheckItem()
        {
            Name = "azure-web-apps",
            FriendlyName = "Azure Web Apps",
            ImageUrl = "https://img.icons8.com/color/48/000000/azure-web-apps.png" // TODO: Azure Storage
        },
        new HealthCheckItem()
        {
            Name = "azure-functions",
            FriendlyName = "Azure Functions",
            ImageUrl = "https://img.icons8.com/color/48/000000/azure-web-apps.png" // TODO: Azure Storage
        }
    };

}