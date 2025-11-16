using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetHealthCheckResponse
{
    [JsonPropertyName("timeStamp")]
    public DateTime TimeStamp {
        get;
        set;
    }

    [JsonPropertyName("healthCheckItems")]
    public IList<HealthCheckItem> HealthCheckItems { get; set; } = new List<HealthCheckItem>()
    {
        new ()
        {
            Name = "azure-web-apps",
            FriendlyName = "Azure Web Apps",
            ImageUrl = "https://img.icons8.com/color/48/000000/azure-web-apps.png", // TODO: Azure Storage
            ImmediateHealthCheckResponse = new HealthCheckItemResponse()
            {
                Name = "azure-web-apps",
                IsHealthy = true,
                TimeStamp = DateTime.UtcNow
            }
        },
        new ()
        {
            Name = "azure-functions",
            FriendlyName = "Azure Functions",
            ImageUrl = "https://img.icons8.com/color/48/000000/azure-web-apps.png", // TODO: Azure Storage
            ImmediateHealthCheckResponse = new HealthCheckItemResponse()
            {
                Name = "azure-functions",
                IsHealthy = true,
                TimeStamp = DateTime.UtcNow
            }
        },
        new()
        {
            Name = "azure-communication-services-email",
            FriendlyName = "Azure Communication Services (Email)",
            ImageUrl = "https://img.icons8.com/color/48/000000/azure-web-apps.png" // TODO: Azure Storage
        },
        new()
        {
            Name = "azure-storage",
            FriendlyName = "Azure Storage",
            ImageUrl = "https://img.icons8.com/color/48/000000/azure-web-apps.png" // TODO: Azure Storage
        },
        new()
        {
            Name = "azure-cosmos-db",
            FriendlyName = "Azure Cosmos DB",
            ImageUrl = "https://img.icons8.com/color/48/000000/azure-web-apps.png" // TODO: Azure Storage
        }
    };

}