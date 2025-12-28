using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.ApplicationDefinitions;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.Scouting.Administration;

public class HealthCheck : ApplicationHealthCheckBase, IHealthCheck
{
    private readonly IUserRepository _userRepository;
    protected readonly ILogger<HealthCheck> _logger;
    protected readonly ApplicationMetaData _applicationMetaData;
    
    public HealthCheck(ApplicationMetaData applicationMetaData, IUserRepository userRepository)
        : base(applicationMetaData, userRepository)
    {
        _userRepository = userRepository;
        _applicationMetaData = applicationMetaData;
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
                await GetHealthCheckForApplicationDefinedInRepository(),
                await GetHealthCheckForAllRolesAcrossAllUsersAsync()
            }
        };
        result.IsHealthy = result.Items.All(q=>q.IsHealthy ?? false);
        result.Message = result.IsHealthy ? "OK" : "Problem(s) found";

        return result;

    }

  
}