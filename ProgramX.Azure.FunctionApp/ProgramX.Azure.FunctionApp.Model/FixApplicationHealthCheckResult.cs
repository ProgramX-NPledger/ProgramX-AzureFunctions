using System.Collections.Specialized;

namespace ProgramX.Azure.FunctionApp.Model;

public class FixApplicationHealthCheckResult
{
    public IList<FixApplicationHealthCheckResultItemResult>? Items { get; set; }
}