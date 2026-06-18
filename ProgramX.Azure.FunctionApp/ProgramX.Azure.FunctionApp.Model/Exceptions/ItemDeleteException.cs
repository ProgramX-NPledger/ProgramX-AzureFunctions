using System.Net;
using System.Runtime.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Exceptions;

public class ItemDeleteException : RepositoryException
{
    public HttpStatusCode HttpStatusCode { get; set; }
    
    public ItemDeleteException()
    {
    }

    public ItemDeleteException(string? message) : base(message)
    {
    }

    public ItemDeleteException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public ItemDeleteException(Type entityType) : base($"Delete of {entityType.Name} failed.")
    {
        OperationType = OperationType.Delete;
        EntityType = entityType;
    }

    public ItemDeleteException(Type entityType, HttpStatusCode httpStatusCode) : base($"Delete of {entityType.Name} failed.")
    {
        OperationType = OperationType.Delete;
        EntityType = entityType;
        HttpStatusCode = httpStatusCode;
    }

    
    public ItemDeleteException(Type entityType, string message) : base($"Delete of {entityType.Name} failed. {message}")
    {
        OperationType = OperationType.Delete;
        EntityType = entityType;
    }

}
