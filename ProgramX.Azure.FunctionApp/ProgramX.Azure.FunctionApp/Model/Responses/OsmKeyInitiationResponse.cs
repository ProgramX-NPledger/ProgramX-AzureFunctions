using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class OsmKeyInitiationResponse
{
    [JsonPropertyName( "osmClientId")]
    public string OsmClientId { get; set; }
    
    [JsonPropertyName( "osmRedirectUri")]
    public string OsmRedirectUri { get; set; }
    
    [JsonPropertyName( "osmScopes")]
    public string OsmScopes { get; set; }
    
    [JsonPropertyName( "url")]
    public string Url { get; set; }
}