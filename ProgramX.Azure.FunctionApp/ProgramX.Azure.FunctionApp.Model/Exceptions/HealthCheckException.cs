using System.Runtime.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Exceptions;

public class HealthCheckException : ApplicationException
{
    public HealthCheckItemResult CurrentHealthCheck { get; init; }
    
    public HealthCheckException(HealthCheckItemResult currentHealthCheck, string? message) : base(message)
    {
        CurrentHealthCheck = currentHealthCheck;
    }

    public HealthCheckException(HealthCheckItemResult currentHealthCheck, string? message, Exception? innerException) : base(message, innerException)
    {
        CurrentHealthCheck = currentHealthCheck;
    }

}