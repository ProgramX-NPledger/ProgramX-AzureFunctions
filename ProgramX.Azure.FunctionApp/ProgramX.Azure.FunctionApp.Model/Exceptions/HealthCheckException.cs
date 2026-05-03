using System.Runtime.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Exceptions;

public class HealthCheckException : ApplicationException
{
    public ServiceHealthCheckItemResult CurrentHealthCheck { get; init; }
    
    public HealthCheckException(ServiceHealthCheckItemResult currentHealthCheck, string? message) : base(message)
    {
        CurrentHealthCheck = currentHealthCheck;
    }

    public HealthCheckException(ServiceHealthCheckItemResult currentHealthCheck, string? message, Exception? innerException) : base(message, innerException)
    {
        CurrentHealthCheck = currentHealthCheck;
    }

}