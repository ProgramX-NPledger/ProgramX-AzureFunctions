using System.Reflection;
using Microsoft.Extensions.Configuration;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.HealthChecks.Applications;

public class AllRequiredRolesAcrossAllUsers : IApplicationHealthCheck
{
    private readonly ApplicationMetaData _applicationMetaData;
    private readonly IUserRepository _userRepository;

    public AllRequiredRolesAcrossAllUsers(
        ApplicationMetaData applicationMetaData,
        IUserRepository userRepository)  
    {
        _applicationMetaData = applicationMetaData;
        _userRepository = userRepository;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        var result = new HealthCheckResult
        {
            FriendlyName = "Required Roles",
            HealthCheckName = nameof(AllRequiredRolesAcrossAllUsers),
        };
        
        var allUsersWithApplication = await _userRepository.GetUsersAsync(new GetUsersCriteria()
        {
            WithRoles = _applicationMetaData.RequiresRoleNames
        });
        
        var missingRoles = new List<string>();
        foreach (var roleName in _applicationMetaData.RequiresRoleNames)
        {
            if (!allUsersWithApplication.Items.Any(q => q.Roles.Contains(roleName)))
            {
                missingRoles.Add(roleName);
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

    public async Task<FixApplicationHealthCheckResult> FixHealthAsync(HealthCheckResult healthCheckResult)
    {
        var allRolesAcrossAllUsersHealthCheckResult = new FixApplicationHealthCheckResult
        {
            Name = nameof(AllRequiredRolesAcrossAllUsers),
            IsSuccess = true,
            Messages = new List<string>()
        };
        
        // create required roles within admin user
        var adminUser = await _userRepository.GetUserByUserNameAsync("admin");
        if (adminUser == null)
        {
            throw new InvalidOperationException("Admin user not found");
        }
        
        // // get the application for Scouting
        // var application = await _userRepository.GetApplicationByNameAsync(_applicationMetaData.Name);
        // if (application == null)
        // {
        //     application = applicationToLinkToRoles;
        //     ((List<string>)allRolesAcrossAllUsersHealthCheckResult.Messages).Add($"Created missing Application {_applicationMetaData.Name} for inclusion in required roles");
        // }
        
        foreach (var requiredRole in _applicationMetaData.RequiresRoleNames)
        {
            // TODO: ensure role doesn't already exist
            // if (await _userRepository.GetRoleByNameAsync(requiredRole) != null) continue;
            //
            // try
            // {
            //     await _userRepository.CreateRoleAsync(new Role()
            //     {
            //         name = requiredRole,
            //         description = $"Created by {nameof(FixHealthAsync)} for {nameof(AllRequiredRolesAcrossAllUsers)}",
            //         applications = new List<Application>
            //         {
            //             CreateApplicationFromMetaData()
            //         }
            //     }, new List<string>
            //     {
            //         adminUser.userName
            //     });
            //     ((List<string>)allRolesAcrossAllUsersHealthCheckResult.Messages).Add($"Added role {requiredRole} and adding to Admin User {adminUser.userName} and adding Application {_applicationMetaData.Name}");
            // }
            // catch (Exception e)
            // {
            //     ((List<string>)allRolesAcrossAllUsersHealthCheckResult.Messages).Add(e.Message);
            //     allRolesAcrossAllUsersHealthCheckResult.IsSuccess = false;
            // }

        }
        return allRolesAcrossAllUsersHealthCheckResult;    
    }

    private Application CreateApplicationFromMetaData()
    {
        return new Application()
        {
            name = _applicationMetaData.Name,
            createdAt = DateTime.Now,
            friendlyName = _applicationMetaData.FriendlyName,
            schemaVersionNumber = 3,
            isDefaultApplicationOnLogin = false,
            ordinal = 1,
            updatedAt = DateTime.Now
        };
    }
}