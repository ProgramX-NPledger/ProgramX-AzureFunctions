using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Osm.Model.Osm.Responses;

public class GetMembersResponse
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }
    [JsonPropertyName("photos")]
    public bool Photos { get; set; }
    [JsonPropertyName("items")]
    public IEnumerable<GetMembersResponseMember> Items { get; set; }
    
    
    
}