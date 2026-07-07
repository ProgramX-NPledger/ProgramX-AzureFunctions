using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.HealthChecks.Applications;

public class AllRequiredRolesDefined : IApplicationHealthCheck
{
    private readonly ApplicationMetaData _applicationMetaData;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<AllRequiredRolesDefined> _logger;

    public AllRequiredRolesDefined(
        ILoggerFactory loggerFactory,
        ApplicationMetaData applicationMetaData,
        IRoleRepository roleRepository)
    {
        _logger = loggerFactory.CreateLogger<AllRequiredRolesDefined>();
        _applicationMetaData = applicationMetaData;
        _roleRepository = roleRepository;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        _logger.LogInformation("Running health check {HealthCheckName}", nameof(AllRequiredRolesDefined));

        var result = new HealthCheckResult
        {
            FriendlyName = "Defined Roles",
            HealthCheckName = nameof(AllRequiredRolesDefined),
        };
        
        var requiredRoleNames = _applicationMetaData.RequiresRoleNames?.ToList() ?? [];

        var missingRoles = new List<string>();
        foreach (var roleName in requiredRoleNames)
        {
            var matchingRoles = await _roleRepository.GetRolesAsync(new GetRolesCriteria()
            {
                AnyOfRoleNames = requiredRoleNames
            });
            if (!matchingRoles.Items.Any())
            {
                missingRoles.Add(roleName);
            }
        }
        
        if (!missingRoles.Any())
        {
            result.IsHealthy = true;
            result.Message = "All Roles defined";
            _logger.LogInformation("Health check {HealthCheckName} passed", nameof(AllRequiredRolesDefined));
        }
        else
        {
            result.IsHealthy = false;
            result.Message = $"Undefined roles: {string.Join(", ", missingRoles)}";
            _logger.LogWarning(
                "Health check {HealthCheckName} failed. Missing roles: {MissingRoles}. These need to be created.",
                nameof(AllRequiredRolesDefined),
                string.Join(", ", missingRoles));
        }
        return result;    
    }

}
