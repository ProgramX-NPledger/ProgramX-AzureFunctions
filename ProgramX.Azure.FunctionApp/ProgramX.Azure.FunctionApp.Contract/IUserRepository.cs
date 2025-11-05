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
    /// <remarks>The return type is of <see cref="SecureUser"/>, which is a subset of <see cref="User"/>, excluding
    /// security data.</remarks>  
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

    /// <summary>
    /// Gets a User by their unique ID.
    /// </summary>
    /// <param name="id">The ID of the user.</param>
    /// <returns>The requested <see cref="User"/>, or <c>null</c> if not found.</returns>
    Task<SecureUser?> GetUserByIdAsync(string id);
    
    /// <summary>
    /// Gets a User by their unique username.
    /// </summary>
    /// <param name="userName">The username of the user.</param>
    /// <returns>The requested <see cref="User"/>, or <c>null</c> if not found.</returns>
    Task<SecureUser?> GetUserByUserNameAsync(string userName);
    
    /// <summary>
    /// Deletes the User with the given ID.
    /// </summary>
    /// <param name="id">The ID of the user to delete.</param>
    Task DeleteUserByIdAsync(string id);

    /// <summary>
    /// Gets a User by their unique ID. This will return a <see cref="User"/>, not a <see cref="SecureUser"/>, including
    /// security data.
    /// </summary>
    /// <param name="id">The ID of the user.</param>
    /// <returns>The requested <see cref="User"/>, or <c>null</c> if not found.</returns>
    Task<User?> GetInsecureUserByIdAsync(string id);
    
    /// <summary>
    /// Gets an Application by its name. This will return a <see cref="Application"/>.
    /// </summary>
    /// <param name="name">The name of the application.</param>
    /// <returns>The requested <see cref="Application"/>, or <c>null</c> if not found.</returns>
    Task<Application?> GetApplicationByNameAsync(string name);
    
    /// <summary>
    /// Update the specified user.
    /// </summary>
    /// <param name="user">The <see cref="User"/> to update.</param>
    Task UpdateUserAsync(SecureUser user);

    /// <summary>
    /// Creates the specified user.
    /// </summary>
    /// <param name="user">User to create.</param>
    Task CreateUserAsync(User user);

    /// <summary>
    /// Creates the specified role and adds to the specified users.
    /// </summary>
    /// <param name="role">Role to create.</param>
    /// <param name="usersInRoles">List of usernames of users to add to Role.</param>
    Task CreateRoleAsync(Role role, IEnumerable<string> usersInRoles);

}