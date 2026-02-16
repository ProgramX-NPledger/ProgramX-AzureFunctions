using System.Runtime.InteropServices.ComTypes;

namespace ProgramX.Azure.FunctionApp.Model.Criteria;

public class GetScoutingActivitiesCriteria
{
    public string? Id { get; set; }
    
    public IEnumerable<ActivityLocation>? AnyOfActivityLocations { get; set; }

    public IEnumerable<ActivityFormat>? AnyOfActivityFormats { get; set; }

    public IEnumerable<ActivityType>? AnyOfActivityTypes { get; set; }

    public IEnumerable<WinMode>? AnyOfWinModes { get; set; }

    public string? ContainingText { get; set; }

    public IEnumerable<int>? ContributesTowardsAnyOsmBadgeId { get; set; }

    public IEnumerable<Section>? AnyOfSections { get; set; }
}