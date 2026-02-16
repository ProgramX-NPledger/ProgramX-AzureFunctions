using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class CreateScoutingActivityRequest
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("activityLocation")]
    public ActivityLocation ActivityLocation { get; set; }
    
    [JsonPropertyName("activityFormat")]
    public ActivityFormat ActivityFormat { get; set; }
    
    [JsonPropertyName("activityType")]
    public ActivityType ActivityType { get; set; }

    [JsonPropertyName("preparationMarkdown")]
    public string? PreparationMarkdown { get; set; }
    
    [JsonPropertyName("referencesMarkdown")]
    public string? ReferencesMarkdown { get; set; }
    
    [JsonPropertyName("winModes")]
    public IEnumerable<WinMode> WinModes { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("summary")]
    public string Summary { get; set; }
    
    [JsonPropertyName("descriptionMarkdown")]
    public string? DescriptionMarkdown { get; set; }
    
    [JsonPropertyName("tags")]
    public IEnumerable<string> Tags { get; set; }
    
    [JsonPropertyName("resources")]
    public IEnumerable<string> Resources { get; set; }

    [JsonPropertyName("sections")]
    public IEnumerable<Section> Sections { get; set; }
    
    [JsonPropertyName("contributesTowardsOsmBadgeId")]
    public int? ContributesTowardsOsmBadgeId { get; set; }
    
    [JsonPropertyName("contributesTowardsOsmBadgeName")]
    public string? ContributesTowardsOsmBadgeName { get; set; }
    
    
}