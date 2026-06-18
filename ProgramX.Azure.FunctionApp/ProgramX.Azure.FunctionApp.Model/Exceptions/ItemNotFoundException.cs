using System.Runtime.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Exceptions;

public class ItemNotFoundException : RepositoryException
{
    
    public ItemNotFoundException()
    {
    }

    public ItemNotFoundException(string? message) : base(message)
    {
    }

    public ItemNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public ItemNotFoundException(OperationType operationType, Type entityType) : base($"Operation {operationType} of {entityType.Name} failed. Item not found.")
    {
        OperationType = operationType;
        EntityType = entityType;
    }
    
    public ItemNotFoundException(OperationType operationType, Type entityType, string message) : base($"Operation {operationType} of {entityType.Name} failed. {message}. Item not found.")
    {
        OperationType = operationType;
        EntityType = entityType;
    }

}
