namespace ProgramX.Azure.FunctionApp.Osm.Model.Criteria;

public class GetMeetingsCriteria
{
    public GetMeetingsSortBy SortBy { get; set; } = GetMeetingsSortBy.Natural;
    public IEnumerable<string> Keywords { get; set; }
    
    public bool? HasOutstandingRequiredParents { get; set; }
    public DateOnly? OccursOnorAfter { get; set; }
    public DateOnly? OccursOnOrBefore { get; set; }
    public bool? HasPrimaryLeader { get; set; }
    public int? SectionId { get; set; }
    public int TermId { get; set; }
    
}