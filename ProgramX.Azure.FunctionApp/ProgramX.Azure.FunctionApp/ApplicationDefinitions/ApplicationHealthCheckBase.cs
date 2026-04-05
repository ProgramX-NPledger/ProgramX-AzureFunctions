using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions;

public abstract class ApplicationHealthCheckBase
{
    protected readonly string AllRolesAcrossAllUsersName = "AllRolesAcrossAllUsers";
        
    protected readonly ApplicationMetaData _applicationMetaData;
    protected readonly IUserRepository _userRepository;
    
    protected ApplicationHealthCheckBase(ApplicationMetaData applicationMetaData, IUserRepository userRepository)
    {
        _applicationMetaData = applicationMetaData;
        _userRepository = userRepository;
    }
    
    protected async Task<HealthCheckItemResult> GetHealthCheckForAllRolesAcrossAllUsersAsync()
    {
        var result = new HealthCheckItemResult()
        {
            FriendlyName = "All Roles across all Users",
            Name = AllRolesAcrossAllUsersName,
        };

        var allUsersWithApplication = await _userRepository.GetUsersAsync(new GetUsersCriteria()
        {
            HasAccessToApplications = [_applicationMetaData.Name],
        });

        var missingRoles = new List<string>();
        foreach (var role in _applicationMetaData.RequiresRoleNames)
        {
            if (!allUsersWithApplication.Items.Any(q => q.roles.Any(r => r.name == role)))
            {
                missingRoles.Add(role);
            }
        }

        if (!missingRoles.Any())
        {
            result.IsHealthy = true;
            result.Message = "All Roles found";
        }
        else
        {
            result.IsHealthy = false;
            result.Message = $"Missing roles: {string.Join(", ", missingRoles)}";
        }
        return result;
    }

    /// <summary>
    /// Creates missing Roles required by the Application. Can be called by derived classes to create Roles.
    /// </summary>
    /// <param name="applicationToLinkToRoles">The Application to link Roles to. This is used if the existing Roles don#t already have the Application defined.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected async Task<FixApplicationHealthCheckResultItemResult> FixHealthCheckForAllRolesAcrossAllUsersAsync(Application applicationToLinkToRoles)
    {
         var allRolesAcrossAllUsersHealthCheckResult = new FixApplicationHealthCheckResultItemResult
        {
            Name = AllRolesAcrossAllUsersName,
            IsSuccess = true,
            Messages = new List<string>()
        };
        
        // create required roles within admin user
        var adminUser = await _userRepository.GetUserByUserNameAsync("admin");
        if (adminUser == null)
        {
            throw new InvalidOperationException("Admin user not found");
        }
        
        // get the application for Scouting
        var application = await _userRepository.GetApplicationByNameAsync(_applicationMetaData.Name);
        if (application == null)
        {
            application = applicationToLinkToRoles;
            ((List<string>)allRolesAcrossAllUsersHealthCheckResult.Messages).Add($"Created missing Application {_applicationMetaData.Name} for inclusion in required roles");
        }
        
        foreach (var requiredRole in _applicationMetaData.RequiresRoleNames)
        {
            // ensure role doesn't already exist
            if (await _userRepository.GetRoleByNameAsync(requiredRole) != null) continue;
            
            try
            {
                await _userRepository.CreateRoleAsync(new Role()
                {
                    name = requiredRole,
                    description = $"Created by {nameof(FixHealthCheckForAllRolesAcrossAllUsersAsync)} for {AllRolesAcrossAllUsersName}",
                    applications = new List<Application>
                    {
                        application
                    }
                }, new List<string>
                {
                    adminUser.userName
                });
                ((List<string>)allRolesAcrossAllUsersHealthCheckResult.Messages).Add($"Added role {requiredRole} and adding to Admin User {adminUser.userName} and adding Application {_applicationMetaData.Name}");
            }
            catch (Exception e)
            {
                ((List<string>)allRolesAcrossAllUsersHealthCheckResult.Messages).Add(e.Message);
                allRolesAcrossAllUsersHealthCheckResult.IsSuccess = false;
            }

        }
        return allRolesAcrossAllUsersHealthCheckResult;
    }
}