
namespace ProgramX.Azure.FunctionApp.Osm.Model;

public class Meeting
{
    public int OsmEveningId { get; set; }
    public string Title { get; set; }
    public int ParentsRequiredCount { get; set; }
    public int ParentsOutstandingCount { get; set; }
    public DateOnly Date { get; set; }
    public ConciseMember? PrimaryLeader { get; set; }
    public IEnumerable<ConciseBadge> Badges { get; set; }

    public int UnavailableLeadersCount { get; set; }
    
    
}