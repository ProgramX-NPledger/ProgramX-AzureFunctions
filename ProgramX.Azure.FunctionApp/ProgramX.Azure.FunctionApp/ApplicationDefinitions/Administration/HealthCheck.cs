using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions.Administration;

public class HealthCheck : ApplicationHealthCheckBase, IHealthCheck
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
        using (_logger.BeginScope("Health Check {HealthCheckName}", nameof(HealthCheck)))
        {
            var result = new HealthCheckResult()
            {
                HealthCheckName = _applicationMetaData.FriendlyName,
                Items = new List<HealthCheckItemResult>()
                {
                    await GetHealthCheckForApplicationDefinedInRepositoryAsync(),
                    await GetHealthCheckForAllRolesAcrossAllUsersAsync()
                }
            };
            result.IsHealthy = result.Items.All(q => q.IsHealthy ?? false);
            result.Message = result.IsHealthy ? "OK" : "Problem(s) found";

            _logger.LogInformation("Health check result {HealthCheckResult}", result);
            
            return result;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> Fix()
    {
        using (_logger.BeginScope("Fix {HealthCheckName}", nameof(HealthCheck)))
        {
            var messages = new List<string>();

            var applicationDefinedInRepositoryHealthCheck =
                await GetHealthCheckForApplicationDefinedInRepositoryAsync();
            if (!applicationDefinedInRepositoryHealthCheck.IsHealthy ?? false)
            {
                messages.AddRange(
                    await FixApplicationDefinedInRepositoryWithAllRolesAsync(typeof(AdministrationApplication)));
            }

            // recheck to see if above has fixed anything
            var allRolesAcrossAllUsersHealthCheck = await GetHealthCheckForAllRolesAcrossAllUsersAsync();
            if (!allRolesAcrossAllUsersHealthCheck.IsHealthy ?? false)
            {
                messages.AddRange(await FixAllRolesAcrossAllUsersAsync(typeof(AdministrationApplication)));
            }

            _logger.LogInformation("Fix result {HealthCheckResult}", messages);
            return messages;
        }    
    }
}