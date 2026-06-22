using System.Runtime.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Exceptions;

public class InvalidPasswordUpdateException : RepositoryException
{
    
    public InvalidPasswordUpdateReason Reason { get; set; }
    

    
    public InvalidPasswordUpdateException(InvalidPasswordUpdateReason reason) : base($"Invalid password update. Reason: {reason}.")
    {
        Reason = reason;
        OperationType = OperationType.Update;
        EntityType = typeof(User);
    }

    public InvalidPasswordUpdateException(InvalidPasswordUpdateReason reason, string message) : base(message)
    {
        Reason = reason;
        OperationType = OperationType.Update;
        EntityType = typeof(User);
    }


    public InvalidPasswordUpdateException(InvalidPasswordUpdateReason reason, string message, Exception? innerException) : base(message, innerException)
    {
        Reason = reason;
        OperationType = OperationType.Update;
        EntityType = typeof(User);   
    }

    
    
}

public enum InvalidPasswordUpdateReason
{
    InvalidConfirmationNonce,
    WeakPassword,
    PasswordResetLinkExpired
}