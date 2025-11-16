using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Contract;

public interface IHealthCheck
{
    Task<HealthCheckResult> CheckHealthAsync();
}