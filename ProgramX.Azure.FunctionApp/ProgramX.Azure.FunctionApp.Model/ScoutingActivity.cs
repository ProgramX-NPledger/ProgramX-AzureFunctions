namespace ProgramX.Azure.FunctionApp.Model;

public class ScoutingActivity
{
    public string id { get; set; }
    public ActivityLocation activityLocation { get; set; }
    public ActivityFormat activityFormat { get; set; }
    public IEnumerable<WinMode> winModes { get; set; }
    public string title { get; set; }
    public IEnumerable<string> resources { get; set; }
    public IEnumerable<string> preparation { get; set; }
    public string summary { get; set; }
    public IEnumerable<string> rules { get; set; }
    public IEnumerable<string> tags { get; set; }
    public IEnumerable<string> references { get; set; }
    public int schemaVersionNumber { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
    public int? contributesTowardsOsmBadgeId { get; set; }
    public string? contributesTowardsOsmBadgeName { get; set; }
    
    
}