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
            HealthCheckName = MetaData.FriendlyName,
            Items = new List<HealthCheckItemResult>()
            {
                await GetHealthCheckForAllRolesAcrossAllUsersAsync()
            }
        };
        result.IsHealthy = result.Items.All(q=>q.IsHealthy ?? false);
        result.Message = result.IsHealthy ? "OK" : "Problem(s) found";

        return result;

    }

    public async Task<HealthCheckItemResult> GetGitHubCommitHashAsync()
    {
        var commitHash = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "unknown";
        return new HealthCheckItemResult
        {
            Name = "GitHubCommitHash",
            IsHealthy = true,
            Message = $"Current commit hash: {commitHash}",
            FriendlyName = "GitHub Commit Hash"
        };
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
        var allApplications = await UserRepository.GetApplicationsAsync(new GetApplicationsCriteria());
        var applicationWithHighestOrdinal = allApplications.Items.OrderByDescending(q => q.ordinal).FirstOrDefault();

        return new Application()
        {
            name = MetaData.Name,
            friendlyName = MetaData.FriendlyName,
            createdAt = DateTime.UtcNow,
            isDefaultApplicationOnLogin = false,
            ordinal = (applicationWithHighestOrdinal?.ordinal ?? 0) + 1,
            schemaVersionNumber = 3,
            updatedAt = DateTime.UtcNow,
            metaDataDotNetAssembly = MetaData.GetType().Assembly.GetName().Name,
            metaDataDotNetType = MetaData.GetType().Name
        };
    }
    
    
}