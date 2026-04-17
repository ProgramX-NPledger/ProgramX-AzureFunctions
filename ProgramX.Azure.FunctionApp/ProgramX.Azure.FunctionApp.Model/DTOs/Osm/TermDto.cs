using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.DTOs.Osm;

public class TermDto
{
    [JsonPropertyName("startDate")]
    public DateOnly StartDate { get; set; }
    
    [JsonPropertyName("endDate")]
    public DateOnly EndDate { get; set; }
    
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("osmTermId")]
    public int OsmTermId { get; set; }

    [JsonPropertyName("masterTerm")]
    public string? MasterTerm { get; set; }

    [JsonPropertyName("isPast")]
    public bool IsPast { get; set; }

    [JsonPropertyName("sectionId")]
    public int SectionId { get; set; }
    
    [JsonPropertyName("isCurrent")]
    public bool IsCurrent { get; set; }
}