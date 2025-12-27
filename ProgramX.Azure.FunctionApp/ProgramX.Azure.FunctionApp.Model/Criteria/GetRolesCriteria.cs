namespace ProgramX.Azure.FunctionApp.Model.Criteria;

/// <summary>
/// Criteria for retrieving Roles.
/// </summary>
public class GetRolesCriteria
{
    /// <summary>
    /// The name of the Role to retrieve.
    /// </summary>
    public string? RoleName { get; set; }
    
    /// <summary>
    /// Retrieve Roles that contain the specified text in their name or description.
    /// </summary>
    public string? ContainingText { get; set; }
    
    /// <summary>
    /// A list of names of applications that the Role is used in.
    /// </summary>
    public IEnumerable<string>? UsedInApplicationNames { get; set; }

    
}