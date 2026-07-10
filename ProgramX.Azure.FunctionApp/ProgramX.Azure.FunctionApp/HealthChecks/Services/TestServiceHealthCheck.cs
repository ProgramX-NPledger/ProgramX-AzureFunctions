using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.HealthChecks.Services;

public class TestServiceHealthCheck : IServiceHealthCheck   
{
    public async Task<ServiceHealthCheckResult> CheckHealthAsync()
    {
        return new ServiceHealthCheckResult()
        {
            HealthCheckName = "Test",
            FriendlyName = "Test"
        };
    }
}