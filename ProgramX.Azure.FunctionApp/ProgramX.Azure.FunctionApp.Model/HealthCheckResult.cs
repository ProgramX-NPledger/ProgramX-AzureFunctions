using System.Collections.Specialized;

namespace ProgramX.Azure.FunctionApp.Model;

public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string? Message { get; set; }
    public required string HealthCheckName { get; set; }
    
    public IList<HealthCheckItemResult>? Items { get; set; }
}