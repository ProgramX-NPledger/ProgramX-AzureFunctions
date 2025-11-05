namespace ProgramX.Azure.FunctionApp.Model.Criteria;

/// <summary>
/// Criteria for retrieving Applications.
/// </summary>
public class GetApplicationsCriteria
{
    /// <summary>
    /// The name of the Application to retrieve.
    /// </summary>
    public string? ApplicationName { get; set; }
    
    /// <summary>
    /// Retrieve Applications that contain the specified text in their name or description.
    /// </summary>
    public string? ContainingText { get; set; }
    
    /// <summary>
    /// A list of names of Roles that the Application is used in.
    /// </summary>
    public IEnumerable<string>? WithinRoles { get; set; }

    /// <summary>
    /// A list of Application Names to retrieve.
    /// </summary>
    public IEnumerable<string>? ApplicationNames { get; set; }
}