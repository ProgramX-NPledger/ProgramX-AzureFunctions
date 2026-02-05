using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class CreateScoutingActivityRequest
{
    public Guid Id { get; set; }
    public ActivityLocation ActivityLocation { get; set; }
    public ActivityFormat ActivityFormat { get; set; }

    public ActivityType ActivityType { get; set; }

    public string? PreparationMarkdown { get; set; }
    
    public string? ReferencesMarkdown { get; set; }
    
    
    public IEnumerable<WinMode> WinModes { get; set; }
    public string Title { get; set; }
    
    
    public string Summary { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public IEnumerable<string> Tags { get; set; }
    public IEnumerable<string> Resources { get; set; }

    public IEnumerable<Section> Sections { get; set; }
    
    public int? ContributesTowardsOsmBadgeId { get; set; }
    public string? ContributesTowardsOsmBadgeName { get; set; }
    
    
}