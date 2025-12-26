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
}