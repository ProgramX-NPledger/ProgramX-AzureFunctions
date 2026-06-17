namespace ProgramX.Azure.FunctionApp.Model.Criteria;

/// <summary>
/// Criteria for retrieving Roles.
/// </summary>
public class GetRolesCriteria
{
    
    /// <summary>
    /// The name of the Role to retrieve.
    /// </summary>
    public IEnumerable<string>? AnyOfRoleNames { get; set; }
    
    /// <summary>
    /// Retrieve Roles that contain the specified text in their name or description.
    /// </summary>
    public string? ContainingText { get; set; }
    
    
}