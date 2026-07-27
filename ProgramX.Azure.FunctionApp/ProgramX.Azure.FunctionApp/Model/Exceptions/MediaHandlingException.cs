namespace ProgramX.Azure.FunctionApp.Model.Exceptions;

public class MediaHandlingException : ApplicationException
{
    public MediaHandlingError MediaHandlingError { get; }

    public MediaHandlingException()
    {
        
    }
    
    public MediaHandlingException(MediaHandlingError mediaHandlingError)
    {
        MediaHandlingError = mediaHandlingError;
    }


}    

public enum MediaHandlingError
{
    MissingContentTypeHeader,
    InvalidContentTypeHeader,
    MultipartContentBoundaryNotDefined,
}

