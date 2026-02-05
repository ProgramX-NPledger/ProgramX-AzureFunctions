using System.Collections.Specialized;

namespace ProgramX.Azure.FunctionApp.Model;

public class ScoutingActivity
{
    public string id { get; set; }
    public ActivityLocation activityLocation { get; set; }
    public ActivityFormat activityFormat { get; set; }
    public ActivityType activityType { get; set; }
    public IEnumerable<WinMode> winModes { get; set; }
    public string title { get; set; }
    public IEnumerable<string> resources { get; set; }
    public string? preparationMarkdown { get; set; }
    public string summary { get; set; }
    public string? descriptionMarkdown { get; set; }
    public IEnumerable<string> tags { get; set; }
    public string? referencesMarkdown { get; set; }
    public int schemaVersionNumber { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
    public int? contributesTowardsOsmBadgeId { get; set; }
    public string? contributesTowardsOsmBadgeName { get; set; }
    public IEnumerable<Section> sections { get; set; }   
    
}