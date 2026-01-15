namespace ProgramX.Azure.FunctionApp.Osm.Model.Criteria;

/// <summary>
/// Determines criteria for retrieving Terms.
/// </summary>
public class GetTermsCriteria
{
    /// <summary>
    /// The ID of the Section to retrieve Terms for. If not specified, the default Section is used,
    /// as defined in the Osm:SectionId configuration.
    /// </summary>
    public int? SectionId { get; set; }

}