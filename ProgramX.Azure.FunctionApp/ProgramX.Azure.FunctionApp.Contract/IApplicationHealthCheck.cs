using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Contract;

/// <summary>
/// Performs health checking for a service or application.
/// </summary>
public interface IApplicationHealthCheck
{
    /// <summary>
    /// Performs a health check.
    /// </summary>
    /// <returns>A result indicating the health status.</returns>
    Task<HealthCheckResult> CheckHealthAsync();
    
    /// <summary>
    /// Attempts to fix the health issues.
    /// </summary>
    /// <param name="healthCheckResult"></param>
    /// <returns></returns>
    Task<FixApplicationHealthCheckResult> FixHealthAsync(HealthCheckResult healthCheckResult);
    
}