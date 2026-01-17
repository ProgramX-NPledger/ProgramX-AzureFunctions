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
}