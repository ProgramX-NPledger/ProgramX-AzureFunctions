namespace ProgramX.Azure.FunctionApp.Model.Osm;

public class Member
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PatrolRoleLevel { get; set; }
    public required PreciseAge? Age { get; set; }
    public required bool IsActive { get; set; }
    public required int OsmScoutId { get; set; }
    public required string FullName { get; set; }

    public Guid? PhotoId { get; set; }

    public int? OsmPatrolId { get; set; }

    public required int OsmSectionId { get; set; }

    public required DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? PatrolNameAndLevel { get; set; }

    public string PatrolName { get; set; }

    public bool? HasInvitations { get; set; }
    
}