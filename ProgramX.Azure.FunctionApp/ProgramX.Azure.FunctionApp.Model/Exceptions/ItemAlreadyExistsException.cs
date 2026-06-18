using System.Net;
using System.Runtime.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Exceptions;

public class ItemAlreadyExistsException : RepositoryException
{
    public ItemAlreadyExistsException()
    {
    }

    public ItemAlreadyExistsException(string? message) : base(message)
    {
    }

    public ItemAlreadyExistsException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public ItemAlreadyExistsException(Type entityType) : base($"Creation of {entityType.Name} failed. Item with same key already exists.")
    {
        OperationType = OperationType.Create;
        EntityType = entityType;
    }

    
    public ItemAlreadyExistsException(Type entityType, string message) : base($"Creation of {entityType.Name} failed. Item with same key already exists. {message}")
    {
        OperationType = OperationType.Create;
        EntityType = entityType;
    }

}
