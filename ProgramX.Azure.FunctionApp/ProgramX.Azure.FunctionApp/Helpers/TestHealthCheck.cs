using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Helpers;

public class TestHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        return new HealthCheckResult()
        {
            IsHealthy = true,
            Message = "OK",
            Items = new List<HealthCheckItemResult>(),
            HealthCheckName = "Test"
        };
    }
}