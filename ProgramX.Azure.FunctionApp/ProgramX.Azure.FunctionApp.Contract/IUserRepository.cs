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
    /// Update the specified user.
    /// </summary>
    /// <param name="userName">User name of User to update.</param>
    /// <param name="currentPassword">The User's current password.</param>
    /// <param name="newPassword">The User's new password.</param>
    /// <param name="passwordConfirmationNonce">Password confirmation nonce to verify request is valid/</param>
    Task<User> UpdateUserPasswordAsync(string userName, string currentPassword, string newPassword, string passwordConfirmationNonce);
    
    /// <summary>
    /// Update the settings for the specified user.
    /// </summary>
    /// <param name="userName">User name of User to update.</param>
    /// <param name="theme">Name of the User's preferred theme. Specify <c>null</c> to use existing.</param>
    Task<User> UpdateUserSettingsAsync(string userName, string? theme);
    
    /// <summary>
    /// Creates the specified user.
    /// </summary>
    /// <param name="userName">The username to identify the User.</param>
    /// <param name="emailAddress">Email address of the User.</param>
    /// <param name="roles">Roles to assign to the User.</param>
    /// <param name="firstName">First name of the User.</param>
    /// <param name="lastName">Last name of the User.</param>
    /// <param name="passwordConfirmationLinkExpiryDate">Date/time of expiration of the password confirmation link.</param>
    Task<User> CreateUserAsync(string userName, string emailAddress, string? firstName, string? lastName, IEnumerable<string> roles, DateTime passwordConfirmationLinkExpiryDate);
    
    /// <summary>
    /// Gets the password hash and salt for the specified user.
    /// </summary>
    /// <param name="userName">The username of the User to get the password for.</param>
    /// <returns>The password hash and salt for the specified user, or <c>null</c> if not found.</returns>
    Task<UserPassword?> GetUserPasswordByUserNameAsync(string userName);
    
    /// <summary>
    /// Adds a Role to a User.
    /// </summary>
    /// <param name="roleName">Name of the Role.</param>
    /// <param name="userName">Name of the User.</param>
    /// <returns>The updated User.</returns>
    Task<User> AddRoleToUserAsync(string roleName, string userName);

    /// <summary>
    /// Removes a Role from a User.
    /// </summary>
    /// <param name="roleName">Name of the Role.</param>
    /// <param name="userName">Name of the User.</param>
    /// <returns>The updated User.</returns>
    Task<User> RemoveRoleFromUserAsync(string roleName, string userName);

    /// <summary>
    /// Update a User.
    /// </summary>
    /// <param name="userName">The username of the User to update.</param>
    /// <param name="emailAddress">The new email address for the User.</param>
    /// <param name="firstName">The new first name for the User.</param>
    /// <param name="lastName">The new last name for the User.</param>
    /// <param name="roles">The new roles for the User.</param>
    /// <returns>The updated User.</returns>
    Task<User> UpdateUserAsync(string userName, string emailAddress, string? firstName, string? lastName,
        IEnumerable<string>? roles);


}