using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Helpers;

public class TestHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync()
    {
        return Task.FromResult(new HealthCheckResult()
        {
            IsHealthy = true,
            Message = "OK",
            Items = new List<HealthCheckItemResult>(),
            HealthCheckName = "Test"
        });
    }

    public async Task<IEnumerable<string>> Fix()
    {
        throw new NotSupportedException("Test health check cannot be fixed automatically");
    }
}