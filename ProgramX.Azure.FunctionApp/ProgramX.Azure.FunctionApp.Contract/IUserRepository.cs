using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.Contract;

/// <summary>
/// Provides data functionality for <see cref="UserPassword"/>s, <see cref="Role"/>s and <see cref="Application"/>s.
/// </summary>
public interface IUserRepository
{

    /// <summary>
    /// Gets Users from the repository.
    /// </summary>
    /// <param name="criteria">The <see cref="GetUsersCriteria"/> to determine which Users to return.</param>
    /// <param name="pagedCriteria">The <see cref="PagedCriteria"/> that determines paging requirements.
    /// Do not specify if paging is not required.</param>
    /// <returns>Matching items.</returns>
    /// <remarks>The return type is of <see cref="User"/>, which is a subset of <see cref="User"/>, excluding
    /// security data.</remarks>  
    Task<IResult<User>> GetUsersAsync(GetUsersCriteria criteria, PagedCriteria? pagedCriteria = null);
    
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
    IEnumerable<User> GetUsersInRole(string roleName, IEnumerable<User> users);

    /// <summary>
    /// Gets a User by their unique ID.
    /// </summary>
    /// <param name="id">The ID of the user.</param>
    /// <returns>The requested <see cref="User"/>, or <c>null</c> if not found.</returns>
    Task<User?> GetUserByIdAsync(string id);
    
    /// <summary>
    /// Gets a User by their unique username.
    /// </summary>
    /// <param name="userName">The username of the user.</param>
    /// <returns>The requested <see cref="User"/>, or <c>null</c> if not found.</returns>
    Task<User?> GetUserByUserNameAsync(string userName);
    
    /// <summary>
    /// Deletes the User with the given ID.
    /// </summary>
    /// <param name="id">The ID of the user to delete.</param>
    Task DeleteUserByIdAsync(string id);

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
    Task UpdateUserAsync(User user);

    /// <summary>
    /// Creates the specified user.
    /// </summary>
    /// <param name="user">User to create.</param>
    Task CreateUserAsync(User user);

    /// <summary>
    /// Creates the specified Application and adds to the specified Roles.
    /// </summary>
    /// <param name="application">Application to create.</param>
    /// <param name="withinRoles">List of Role names to add Application to.</param>
    Task CreateApplicationAsync(Application application, IEnumerable<string> withinRoles);
    
    /// <summary>
    /// Update the specified Application.
    /// </summary>
    /// <param name="applicationName">The Application Name of the Application to update.</param>   
    /// <param name="application">The updated <see cref="Application"/>.</param>
    Task UpdateApplicationAsync(string applicationName, Application application);
    
    /// <summary>
    /// Deletes the Application with the given name.
    /// </summary>
    /// <param name="applicationName">The name of the Application to delete.</param>
    Task DeleteApplicationByNameAsync(string applicationName);
    
    /// <summary>
    /// Adds the specified Role to the specified User.
    /// </summary>
    /// <param name="roleName">The Role to add to the User.</param>
    /// <param name="userName">The username of the User to add the Role to.</param>
    Task AddUserToRole(Role roleName, string userName);
    
    /// <summary>
    /// Removes the specified Role from the specified User.
    /// </summary>
    /// <param name="roleName">The name of the Role to remove from the User.</param>
    /// <param name="userName">The username of the User to remove the Role from.</param>
    Task RemoveUserFromRole(string roleName, string userName);


    /// <summary>
    /// Updates the password hash and salt for the specified user.
    /// </summary>
    /// <param name="userName">The username of the User to update the password for.</param>
    /// <param name="newPassword">The new password for the user.</param>
    /// <remarks>
    /// If the user has not previously set a password, a new salt will be generated and stored alongside the password hash.
    /// </remarks>
    Task UpdateUserPasswordAsync(string userName, string newPassword);
    
    /// <summary>
    /// Gets the password hash and salt for the specified user.
    /// </summary>
    /// <param name="userName">The username of the User to get the password for.</param>
    /// <returns>The password hash and salt for the specified user, or <c>null</c> if not found.</returns>
    Task<UserPassword?> GetUserPasswordByUserNameAsync(string userName);

    
    /// <summary>
    /// Resets the application.
    /// </summary>
    Task ResetApplicationAsync();
    
    
    
    
}