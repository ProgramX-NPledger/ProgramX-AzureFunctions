using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Osm.Model.Osm.Responses;

public class Leader
{
    [JsonPropertyName( "member_id")]
    public int MemberId { get; set; }
    
    [JsonPropertyName("photo_guid")]
    public Guid? PhotoId { get; set; }
    
    [JsonPropertyName( "first_name")]
    public string FirstName { get; set; }
    
    [JsonPropertyName( "last_name")]
    public string LastName { get; set; }
}