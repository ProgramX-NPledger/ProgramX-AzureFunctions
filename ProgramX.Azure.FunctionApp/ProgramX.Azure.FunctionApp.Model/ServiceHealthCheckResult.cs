using System.Collections.Specialized;

namespace ProgramX.Azure.FunctionApp.Model;

public class ServiceHealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string? Message { get; set; }
    public required string HealthCheckName { get; set; }

    public required string FriendlyName { get; set; }
    
    public IEnumerable<ServiceHealthCheckItemResult>? Items { get; set; }
    
}