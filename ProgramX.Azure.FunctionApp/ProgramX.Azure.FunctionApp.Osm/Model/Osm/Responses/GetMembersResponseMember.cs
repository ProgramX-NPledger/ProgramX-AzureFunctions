using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Osm.Model.Osm.Responses;

public class GetMembersResponseMember
{
    [JsonPropertyName("firstname")]
    public string FirstName { get; set; }
    
    [JsonPropertyName("lastname")]
    public string LastName { get; set; }
    
    [JsonPropertyName("photo_guid")]
    public Guid? PhotoGuid { get; set; }
    
    [JsonPropertyName("patrolid")]
    public int OsmPatrolId { get; set; }
    
    [JsonPropertyName("patrol")]
    public string PatrolName { get; set; } = string.Empty;
    
    [JsonPropertyName("sectionid")]
    public int OsmSectionId { get; set; }
    
    [JsonPropertyName("startdate")]
    public DateOnly StartDate { get; set; }
    
    [JsonPropertyName("enddate")]
    public DateOnly? EndDate { get; set; }
    
    [JsonPropertyName("age")]
    public string Age { get; set; }

    [JsonPropertyName("patrol_role_level_label")]
    public string PatrolRoleLevelLabel { get; set; } = string.Empty;
    
    [JsonPropertyName("active")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("scoutid")]
    public int OsmScoutId { get; set; }
    
    [JsonPropertyName("full_name")]
    public string FullName { get; set; }

    [JsonPropertyName("has_invitations")]
    public bool HasInvitations { get; set; }
    
    
}