using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Osm.Model;

public class GetTermsResponseTerm
{
    [JsonPropertyName( "termid")]
    public int TermId { get; set; }
    [JsonPropertyName("sectionid")]
    public string SectionId { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("startdate")]
    public DateOnly StartDate { get; set; }

    [JsonPropertyName("enddate")]
    public DateOnly EndDate { get; set; }
    [JsonPropertyName("master_term")]
    public string? MasterTerm { get; set; }

    [JsonPropertyName("past")]
    public bool Past { get; set; }
}