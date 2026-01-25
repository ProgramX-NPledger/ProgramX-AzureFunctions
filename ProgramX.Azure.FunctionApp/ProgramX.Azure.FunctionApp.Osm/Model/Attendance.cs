using System.Diagnostics;
using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Osm.Model;

public class Attendance
{
    [JsonPropertyName("firstName")]
    public required string FirstName { get; set; }
    
    [JsonPropertyName("lastName")]
    public required string LastName { get; set; }
    
    [JsonPropertyName("osmPhotoGuid")]
    public Guid? OsmPhotoGuid { get; set; }
    
    [JsonPropertyName("osmPatrolId")]
    public int OsmPatrolId { get; set; }
    
    [JsonPropertyName("isPatrolLeader")]
    public bool IsPatrolLeader { get; set; }
    
    [JsonPropertyName("patrolName")]
    public required string PatrolName { get; set; }
    
    [JsonPropertyName("dateOfBirth")]
    public DateOnly? DateOfBirth { get; set; }
    
    [JsonPropertyName("osmSectionId")]
    public int OsmSectionId { get; set; }
    
    [JsonPropertyName("endDate")]
    public DateOnly? EndDate { get; set; }
    
    [JsonPropertyName( "startDate")]
    public DateOnly StartDate { get; set; }
    
    [JsonPropertyName("age")]
    public PreciseAge? Age { get; set; }
    
    [JsonPropertyName("patrolRoleLevelLabel")]
    public required string PatrolRoleLevelLabel { get; set; }
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("osmScoutId")]
    public int OsmScoutId { get; set; }
    
    [JsonPropertyName("attendanceOverTerm")]
    public IDictionary<DateOnly,bool> AttendanceOverTerm { get; set; }
}