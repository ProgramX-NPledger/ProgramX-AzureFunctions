using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Contract;

/// <summary>
/// Provides a mechanism for retrieving Roles.
/// </summary>
public interface IRolesProvider
{
    /// <summary>
    /// Gets the Roles defined.
    /// </summary>
    /// <returns>A collection of <see cref="Role"/> objects.</returns>
    Task<IEnumerable<Role>> GetRolesAsync();
}