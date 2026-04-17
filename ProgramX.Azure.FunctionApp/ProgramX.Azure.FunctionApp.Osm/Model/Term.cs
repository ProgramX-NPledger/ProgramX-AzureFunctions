namespace ProgramX.Azure.FunctionApp.Model.Osm;

public class Term
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public required string Name { get; set; }
    public int OsmTermId { get; set; }

    public string? MasterTerm { get; set; }

    public bool IsPast { get; set; }

    public int SectionId { get; set; }
    
    public bool IsCurrent => StartDate <= DateOnly.FromDateTime(DateTime.Now) && EndDate >= DateOnly.FromDateTime(DateTime.Now);
    
}