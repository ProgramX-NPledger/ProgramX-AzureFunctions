namespace ProgramX.Azure.FunctionApp.Model.Requests;

/// <summary>
/// Represents a request to create a Role.
/// </summary>
public class CreateRoleRequest
{
    /// <summary>
    /// Name of the Role.
    /// </summary>
    public required string name { get; set; }
    
    /// <summary>
    /// Description of the Role.
    /// </summary>
    public string? description { get; set; }

    /// <summary>
    /// The Users to add to the Role.
    /// </summary>
    public IEnumerable<string> addToUsers { get; set; } = [];

    /// <summary>
    /// The Applications to add the Role to.
    /// </summary>
    public IEnumerable<string> addToApplications { get; set; } = [];

}