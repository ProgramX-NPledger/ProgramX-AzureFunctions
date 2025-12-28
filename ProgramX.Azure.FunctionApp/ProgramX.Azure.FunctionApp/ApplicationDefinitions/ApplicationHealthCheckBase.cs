using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions;

public abstract class ApplicationHealthCheckBase
{
    protected readonly ApplicationMetaData _applicationMetaData;
    private readonly IUserRepository _userRepository;

    protected ApplicationHealthCheckBase(ApplicationMetaData applicationMetaData, IUserRepository userRepository)
    {
        _applicationMetaData = applicationMetaData;
        _userRepository = userRepository;
    }
    
    protected async Task<HealthCheckItemResult> GetHealthCheckForApplicationDefinedInRepositoryAsync()
    {
        var result = new HealthCheckItemResult()
        {
            FriendlyName = "Application is known in repository",
            Name = "ApplicationDefinedInRepository",
        };

        var allUsersWithApplication = await _userRepository.GetUsersAsync(new GetUsersCriteria()
        {
            HasAccessToApplications = [_applicationMetaData.name],
        });

        if (allUsersWithApplication.Items.Any())
        {
            result.IsHealthy = true;
            result.Message = "Application found";
        }
        else
        {
            result.IsHealthy = false;
            result.Message = "Application not found";
        }
        return result;
    }

    protected async Task<HealthCheckItemResult> GetHealthCheckForAllRolesAcrossAllUsersAsync()
    {
        var result = new HealthCheckItemResult()
        {
            FriendlyName = "All Roles across all Users",
            Name = "AllRolesAcrossAllUsers",
        };

        var allUsersWithApplication = await _userRepository.GetUsersAsync(new GetUsersCriteria()
        {
            HasAccessToApplications = [_applicationMetaData.name],
        });

        var missingRoles = new List<string>();
        foreach (var role in _applicationMetaData.requiresRoleNames)
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
    /// Grants the first User defined in the Repository all required Roles, which includes the Application
    /// </summary>
    /// <returns></returns>
    protected async Task<IEnumerable<string>> FixApplicationDefinedInRepositoryWithAllRolesAsync(
        Type applicationType
        )
    {
        var messages = new List<string>();
        
        var allUsersWithApplication = await _userRepository.GetUsersAsync(new GetUsersCriteria()
        {
            HasAccessToApplications = [_applicationMetaData.name],
        });

        if (allUsersWithApplication.Items.Any())
        {
            throw new InvalidOperationException("Application already defined in repository, fixing is not required or appropriate");
        }

        // get the first user in the repository
        var firstUser = _userRepository.GetUsersAsync(new GetUsersCriteria()).Result.Items.FirstOrDefault();
        if (firstUser == null) throw new InvalidOperationException("No users found in repository, cannot create application");
        messages.Add($"Adding to first user found: Username {firstUser.userName}");
        
        foreach (var role in _applicationMetaData.requiresRoleNames)
        {
            var newRole = new Role()
            {
                name = role,
                applications =
                [
                    new Application()
                    {
                        name = _applicationMetaData.name,
                        metaDataDotNetAssembly = applicationType.Assembly.GetName().Name!,
                        metaDataDotNetType = applicationType.FullName!,
                        createdAt = DateTime.UtcNow,
                        updatedAt = DateTime.UtcNow,
                        isDefaultApplicationOnLogin = false,
                        ordinal = 1
                    }
                ],
                schemaVersionNumber = 2,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow,
                description = "Automatically created role for application"
            };
            await _userRepository.CreateRoleAsync(newRole, [firstUser.userName]);
            messages.Add($"Added role {role} with application {_applicationMetaData.name} to user {firstUser.userName}");       
        }
        return messages;
    }

    protected async Task<IEnumerable<string>> FixAllRolesAcrossAllUsersAsync(Type applicationType)
    {
          var messages = new List<string>();
        
        var allUsersWithApplication = await _userRepository.GetUsersAsync(new GetUsersCriteria()
        {
            HasAccessToApplications = [_applicationMetaData.name],
        });

        if (!allUsersWithApplication.Items.Any())
        {
            throw new InvalidOperationException("No users have the Application defined, cannot fix");
        }
        
        // get the first user in the repository
        var firstUser = allUsersWithApplication.Items.First();
        messages.Add($"Adding to first user found that has the Application: Username {firstUser.userName}");
        
        foreach (var role in _applicationMetaData.requiresRoleNames)
        {
            var newRole = new Role()
            {
                name = role,
                applications =
                [
                    new Application()
                    {
                        name = _applicationMetaData.name,
                        metaDataDotNetAssembly = applicationType.Assembly.GetName().Name!,
                        metaDataDotNetType = applicationType.FullName!,
                        createdAt = DateTime.UtcNow,
                        updatedAt = DateTime.UtcNow,
                        isDefaultApplicationOnLogin = false,
                        ordinal = 1
                    }
                ],
                schemaVersionNumber = 2,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow,
                description = "Automatically created role for application"
            };
            await _userRepository.CreateRoleAsync(newRole, [firstUser.userName]);
            messages.Add($"Added role {role} with application {_applicationMetaData.name} to user {firstUser.userName}");       
        }
        return messages;
        
        
        
    }
}