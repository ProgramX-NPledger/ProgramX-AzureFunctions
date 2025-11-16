namespace ProgramX.Azure.FunctionApp.Model;

public class HealthCheckItemResult
{
    /// <summary>
    /// Whether the check is healthy. If <c>null</c>, the check has not been run yet.
    /// </summary>
    public bool? IsHealthy { get; set; }
    
    /// <summary>
    /// Friendly text describing the check.
    /// </summary>
    public required string FriendlyName { get; set; }
    
    /// <summary>
    /// Additional information about the check.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Name of the check
    /// </summary>
    public required string Name { get; set; }
}