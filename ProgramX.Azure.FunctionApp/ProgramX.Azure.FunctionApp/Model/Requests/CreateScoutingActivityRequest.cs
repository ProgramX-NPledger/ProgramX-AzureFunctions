using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class CreateScoutingActivityRequest
{
    public Guid Id { get; set; }
    public ActivityLocation ActivityLocation { get; set; }
    public ActivityFormat ActivityFormat { get; set; }
    public IEnumerable<WinMode> WinModes { get; set; }
    public string Title { get; set; }
    public IEnumerable<string> Resources { get; set; }
    public IEnumerable<string> Preparation { get; set; }
    public string Summary { get; set; }
    public IEnumerable<string> Rules { get; set; }
    public IEnumerable<string> Tags { get; set; }
    public IEnumerable<string> References { get; set; }
    
    public int? ContributesTowardsOsmBadgeId { get; set; }
    public string? ContributesTowardsOsmBadgeName { get; set; }
    
    
}