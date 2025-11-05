namespace ProgramX.Azure.FunctionApp.Model.Criteria;

/// <summary>
/// Criteria for retrieving Roles.
/// </summary>
public class GetUsersCriteria
{
    /// <summary>
    /// The unique ID of the User to retrieve.
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// The username of the User to retrieve.
    /// </summary>
    public string? UserName { get; set; }
    
    /// <summary>
    /// Retrieve Roles that contain the specified text in their name or description.
    /// </summary>
    public string? ContainingText { get; set; }
    
    /// <summary>
    /// A list of Role Names returned Users must be a member of.
    /// </summary>
    public IEnumerable<string>? WithRoles { get; set; }
    
    /// <summary>
    /// A list of Application Names returned Users must have access to.
    /// </summary>
    public IEnumerable<string>? HasAccessToApplications { get; set; }

    /// <summary>
    /// A list of User Names to retrieve.
    /// </summary>
    public IEnumerable<string>? UserNames { get; set; }
}