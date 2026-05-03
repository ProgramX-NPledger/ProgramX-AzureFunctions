using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Contract;

/// <summary>
/// Performs health checking for a service or application.
/// </summary>
public interface IServiceHealthCheck
{
    /// <summary>
    /// Performs a health check.
    /// </summary>
    /// <returns>A result indicating the health status.</returns>
    Task<ServiceHealthCheckResult> CheckHealthAsync();

    
}