using System.Runtime.InteropServices.ComTypes;

namespace ProgramX.Azure.FunctionApp.Model.Criteria;

public class GetScoutingScoreItemsCriteria
{
    public string? Id { get; set; }

    public DateOnly? OnOrAfter { get; set; }

    public DateOnly? OnOrBefore { get; set; }
    
    public string[]? PatrolNames { get; set; }

    public string[]? ScoreIds { get; set; }

    public int[]? OsmMemberIds { get; set; }
    
    

}