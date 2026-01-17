using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Osm.Model.Osm.Responses;

public class Badge
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("img")]
    public string ImagePath { get; set; }
}