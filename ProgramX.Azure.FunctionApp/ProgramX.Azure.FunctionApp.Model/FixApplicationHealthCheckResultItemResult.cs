namespace ProgramX.Azure.FunctionApp.Model;

public class FixApplicationHealthCheckResultItemResult
{
    public required string Name { get; set; }
    public required bool IsSuccess { get; set; }
    public required IEnumerable<string> Messages { get; set; } = [];
}