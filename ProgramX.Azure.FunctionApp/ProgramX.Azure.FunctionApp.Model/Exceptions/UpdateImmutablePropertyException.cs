using System.Runtime.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Exceptions;

public class UpdateImmutablePropertyException : RepositoryException
{
    
    public UpdateImmutablePropertyException()
    {
    }

    public UpdateImmutablePropertyException(string? message) : base(message)
    {
    }

    public UpdateImmutablePropertyException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public UpdateImmutablePropertyException(OperationType operationType, Type entityType) : base($"Operation {operationType} of {entityType.Name} failed. Attempted to update immutable property.")
    {
        OperationType = operationType;
        EntityType = entityType;
    }
    
    public UpdateImmutablePropertyException(OperationType operationType, Type entityType, string message) : base($"Operation {operationType} of {entityType.Name} failed. Attempted to update immutable property. {message}")
    {
        OperationType = operationType;
        EntityType = entityType;
    }

    
    public UpdateImmutablePropertyException(OperationType operationType, Type entityType, string message, string immutableProperty) : base($"Operation {operationType} of {entityType.Name} failed. Attempted to update immutable property {immutableProperty}.")
    {
        OperationType = operationType;
        EntityType = entityType;
    }

    
}
