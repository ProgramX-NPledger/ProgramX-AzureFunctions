using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.Contract;

/// <summary>
/// Provides data functionality for Roles.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Gets Roles from the repository.
    /// </summary>
    /// <param name="criteria">The <see cref="GetRolesCriteria"/> to determine which Roles to return.</param>
    /// <param name="pagedCriteria">The <see cref="PagedCriteria"/> that determines paging requirements.
    /// Do not specify if paging is not required.</param>
    /// <returns>Matching items.</returns>
    /// <remarks>Roles are not an outermost model, therefore they cannot be sorted.</remarks>   
    Task<IResult<Role>> GetRolesAsync(GetRolesCriteria criteria, PagedCriteria? pagedCriteria = null);
    
    /// <summary>
    /// Gets a Role by their unique name.
    /// </summary>
    /// <param name="roleName">The name of the Role.</param>
    /// <returns>The requested <see cref="Role"/>, or <c>null</c> if not found.</returns>
    Task<Role?> GetRoleByNameAsync(string roleName);
    
    /// <summary>
    /// Deletes the Role with the given name.
    /// </summary>
    /// <param name="roleName">The name of the Role to delete.</param>
    Task DeleteRoleByNameAsync(string roleName);
    
    /// <summary>
    /// Creates the specified Role and adds to the specified users.
    /// </summary>
    /// <param name="role">Role to create.</param>
    Task CreateRoleAsync(Role role);

    /// <summary>
    /// Update the specified role. 
    /// </summary>
    /// <param name="roleName">Thename of the Role to update.</param>   
    /// <param name="role">The updated <see cref="Role"/>.</param>
    Task UpdateRoleAsync(Role role);

    
}