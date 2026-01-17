namespace ProgramX.Azure.FunctionApp.Osm.Model.Criteria;

public class GetMembersCriteria
{
    public GetMembersSortBy SortBy { get; set; } = GetMembersSortBy.DateOfBirth;
    public int TermId { get; set; }
    public string? SectionName { get; set; } = "scouts";

    public int? SectionId { get; set; }
    
}