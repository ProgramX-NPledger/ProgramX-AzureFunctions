using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.HealthChecks;
using ProgramX.Azure.FunctionApp.HealthChecks.Applications;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp;

public class ApplicationHealthChecker
{
    private readonly IUserRepository _userRepository;

    public ApplicationHealthChecker(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    /// <summary>
    /// Performs health checks for an application.
    /// </summary>
    /// <param name="application"></param>
    /// <returns></returns>
    public async Task<IEnumerable<HealthCheckResult>> CheckHealthAsync(IApplication application)
    {
        var allHealthChecks = application.GetHealthChecks().Union(new List<IApplicationHealthCheck>
        {
            new AllRequiredRolesAcrossAllUsers(application.GetApplicationMetaData(), _userRepository)
        });
        
        var healthCheckResults = new List<HealthCheckResult>();
        
        foreach (var healthCheck in allHealthChecks)
        {
            healthCheckResults.Add(await healthCheck.CheckHealthAsync());
        }
        
        return healthCheckResults;
    }
}