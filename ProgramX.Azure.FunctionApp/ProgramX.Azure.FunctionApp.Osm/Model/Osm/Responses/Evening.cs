using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Osm.JsonConverters;

namespace ProgramX.Azure.FunctionApp.Osm.Model.Osm.Responses;

public class Evening
{
    [JsonPropertyName( "eveningid")]
    public int EveningId { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("parentsrequired")]
    public int ParentsRequiredCount { get; set; }
    
    [JsonPropertyName("meetingdate")]
    public DateOnly MeetingDate { get; set; }
    
    [JsonPropertyName("parentsattendingcount")]
    public int ParentsAttendingCount { get; set; }

    [JsonPropertyName( "primary_leader")]
    [JsonConverter(typeof(BooleanOrLeaderJsonPropertyConverter))]
    public Leader? PrimaryLeader { get; set; }

    [JsonPropertyName("badges")]
    public IEnumerable<Badge>? Badges { get; set; } = Enumerable.Empty<Badge>();

    [JsonPropertyName( "unavailable_leaders")]
    public int UnavailableLeaders { get; set; }
    
}