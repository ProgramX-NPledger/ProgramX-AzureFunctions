using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions.Administration;

public class HealthCheck : ApplicationHealthCheckBase, IApplicationHealthCheck
{
    protected readonly ILogger<HealthCheck> _logger;
    
    public HealthCheck(ApplicationMetaData applicationMetaData, IUserRepository userRepository)
        : base(applicationMetaData, userRepository)
    {
        _logger = new LoggerFactory().CreateLogger<HealthCheck>();
    }
    
    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        var result = new HealthCheckResult()
        {
            HealthCheckName = _applicationMetaData.FriendlyName,
            Items = new List<HealthCheckItemResult>()
            {
                await GetHealthCheckForAllRolesAcrossAllUsersAsync()
            }
        };
        result.IsHealthy = result.Items.All(q=>q.IsHealthy ?? false);
        result.Message = result.IsHealthy ? "OK" : "Problem(s) found";

        return result;

    }

    
    /// <inheritdoc/>
    public async Task<FixApplicationHealthCheckResult> FixHealthAsync(HealthCheckResult healthCheckResult)
    {
        List<FixApplicationHealthCheckResultItemResult> fixedItems = new List<FixApplicationHealthCheckResultItemResult>();
        
        // check for required roles
        var allRolesAcrossAllUsersHealthCheck = healthCheckResult.Items.SingleOrDefault(q => q.Name == "AllRolesAcrossAllUsers");
        if (allRolesAcrossAllUsersHealthCheck != null && allRolesAcrossAllUsersHealthCheck.IsHealthy == false)
        {
            var result = await FixHealthCheckForAllRolesAcrossAllUsersAsync(await CreateApplicationAsync());
            fixedItems.Add(result);
        }

        return new FixApplicationHealthCheckResult()
        {
            Items = fixedItems
        };
    }
    
    private async Task<Application> CreateApplicationAsync()
    {
        var allApplications = await _userRepository.GetApplicationsAsync(new GetApplicationsCriteria());
        var applicationWithHighestOrdinal = allApplications.Items.OrderByDescending(q => q.ordinal).FirstOrDefault();

        return new Application()
        {
            name = _applicationMetaData.Name,
            friendlyName = _applicationMetaData.FriendlyName,
            createdAt = DateTime.UtcNow,
            isDefaultApplicationOnLogin = false,
            ordinal = (applicationWithHighestOrdinal?.ordinal ?? 0) + 1,
            schemaVersionNumber = 3,
            updatedAt = DateTime.UtcNow,
            metaDataDotNetAssembly = _applicationMetaData.GetType().Assembly.GetName().Name,
            metaDataDotNetType = _applicationMetaData.GetType().Name
        };
    }
    
    
}