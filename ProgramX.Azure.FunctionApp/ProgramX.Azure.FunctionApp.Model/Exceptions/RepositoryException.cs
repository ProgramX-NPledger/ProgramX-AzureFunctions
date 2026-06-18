using System.Runtime.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Exceptions;

public class RepositoryException : ApplicationException
{
    
    public RepositoryException()
    {
    }

    public RepositoryException(string? message) : base(message)
    {
    }

    public RepositoryException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public RepositoryException(OperationType operationType, Type entityType) : base($"Operation {operationType} of {entityType.Name} failed.")
    {
        OperationType = operationType;
        EntityType = entityType;
    }
    
    public RepositoryException(OperationType operationType, Type entityType, string message) : base($"Operation {operationType} of {entityType.Name} failed. {message}")
    {
        OperationType = operationType;
        EntityType = entityType;
    }

    public OperationType OperationType { get; protected set; }
    public Type? EntityType { get; protected set; }
}

public enum OperationType
{
    Create,
    Read,
    Update,
    Delete
}