using System.Net;
using System.Runtime.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Exceptions;

public class ItemCreationException : RepositoryException
{
    public HttpStatusCode HttpStatusCode { get; set; }
    
    public ItemCreationException()
    {
    }

    public ItemCreationException(string? message) : base(message)
    {
    }

    public ItemCreationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public ItemCreationException(Type entityType) : base($"Creation of {entityType.Name} failed.")
    {
        OperationType = OperationType.Create;
        EntityType = entityType;
    }

    public ItemCreationException(Type entityType, HttpStatusCode httpStatusCode) : base($"Creation of {entityType.Name} failed.")
    {
        OperationType = OperationType.Create;
        EntityType = entityType;
        HttpStatusCode = httpStatusCode;
    }

    
    public ItemCreationException(Type entityType, string message) : base($"Creation of {entityType.Name} failed. {message}")
    {
        OperationType = OperationType.Create;
        EntityType = entityType;
    }

}
