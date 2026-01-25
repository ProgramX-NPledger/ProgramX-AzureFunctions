namespace ProgramX.Azure.FunctionApp.Osm.Model.Criteria;

public class GetAttendanceCriteria
{
    public GetAttendanceSortBy SortBy { get; set; } = GetAttendanceSortBy.Natural;
    
    
    public int? SectionId { get; set; }
    public int? TermId { get; set; }

    public DateOnly? OnOrAfter { get; set; }

    public DateOnly? OnOrBefore { get; set; }

    public int? MemberId { get; set; }
}