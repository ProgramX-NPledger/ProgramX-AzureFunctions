using System.Net;
using System.Runtime.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Exceptions;

public class ItemUpdateException : RepositoryException
{
    public HttpStatusCode HttpStatusCode { get; set; }
    
    public ItemUpdateException()
    {
    }

    public ItemUpdateException(string? message) : base(message)
    {
    }

    public ItemUpdateException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public ItemUpdateException(Type entityType) : base($"Update of {entityType.Name} failed.")
    {
        OperationType = OperationType.Update;
        EntityType = entityType;
    }

    public ItemUpdateException(Type entityType, HttpStatusCode httpStatusCode) : base($"Update of {entityType.Name} failed.")
    {
        OperationType = OperationType.Update;
        EntityType = entityType;
        HttpStatusCode = httpStatusCode;
    }

    
    public ItemUpdateException(Type entityType, string message) : base($"Update of {entityType.Name} failed. {message}")
    {
        OperationType = OperationType.Update;
        EntityType = entityType;
    }

}
