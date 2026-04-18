using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.DTOs.Osm;

public class MemberDto
{
    [JsonPropertyName("firstName")]
    public required string FirstName { get; set; }
    
    [JsonPropertyName("lastName")]
    public required string LastName { get; set; }
    
    [JsonPropertyName("patrolRoleLevel")]
    public required string PatrolRoleLevel { get; set; }
    
    [JsonPropertyName("age")]
    public required PreciseAge? Age { get; set; }

    [JsonPropertyName("isActive")]
    public required bool IsActive { get; set; }
    
    [JsonPropertyName("osmScoutId")]
    public required int OsmScoutId { get; set; }
    
    [JsonPropertyName("fullName")]
    public required string FullName { get; set; }

    [JsonPropertyName("photoId")]
    public Guid? PhotoId { get; set; }

    [JsonPropertyName("osmPatrolId")]
    public int? OsmPatrolId { get; set; }

    [JsonPropertyName("osmSectionId")]
    public required int OsmSectionId { get; set; }

    // [JsonPropertyName("patrolRoleLevelLabel")]
    // public required string PatrolRoleLevelLabel { get; set; }

    [JsonPropertyName("startDate")]
    public required DateOnly StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateOnly? EndDate { get; set; }

    [JsonPropertyName("patrolNamAndLevel")]
    public string? PatrolNameAndLevel { get; set; }

    [JsonPropertyName("patrolName")]
    public string PatrolName { get; set; }

    [JsonPropertyName("hasInvitations")]
    public bool? HasInvitations { get; set; }
    
}