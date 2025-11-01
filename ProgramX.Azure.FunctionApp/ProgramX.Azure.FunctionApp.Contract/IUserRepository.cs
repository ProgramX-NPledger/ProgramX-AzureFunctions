using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.Contract;

/// <summary>
/// Provides data functionality for <see cref="User"/>s, <see cref="Role"/>s and <see cref="Application"/>s.
/// </summary>
public interface IUserRepository
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
    /// Gets Users from the repository.
    /// </summary>
    /// <param name="criteria">The <see cref="GetUsersCriteria"/> to determine which Users to return.</param>
    /// <param name="pagedCriteria">The <see cref="PagedCriteria"/> that determines paging requirements.
    /// Do not specify if paging is not required.</param>
    /// <returns>Matching items.</returns>
    Task<IResult<SecureUser>> GetUsersAsync(GetUsersCriteria criteria, PagedCriteria? pagedCriteria = null);
    
    /// <summary>
    /// Gets Applications from the repository.
    /// </summary>
    /// <param name="criteria">The <see cref="GetApplicationsCriteria"/> to determine which Applications to return.</param>
    /// <param name="pagedCriteria">The <see cref="PagedCriteria"/> that determines paging requirements.
    /// Do not specify if paging is not required.</param>
    /// <returns>Matching items.</returns>
    Task<IResult<Application>> GetApplicationsAsync(GetApplicationsCriteria criteria, PagedCriteria? pagedCriteria = null);

    /// <summary>
    /// Given a role name and a list of users, returns the users that are in that role.
    /// </summary>
    /// <param name="roleName">Name of the Role.</param>
    /// <param name="users">A list of Users to verify membership/</param>
    /// <returns>Matching items.</returns>
    IEnumerable<SecureUser> GetUsersInRole(string roleName, IEnumerable<SecureUser> users);

}