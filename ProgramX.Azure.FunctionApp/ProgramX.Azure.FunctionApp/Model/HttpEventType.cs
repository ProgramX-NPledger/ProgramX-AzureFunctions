namespace ProgramX.Azure.FunctionApp.Model;

public enum HttpEventType
{
    /// <summary>
    /// The request was sent out over the wire.
    /// </summary>
    Sent = 0,

    /// <summary>
    /// An upload progress event was received. 
    /// </summary>
    UploadProgress = 1,
    
    /// <summary>
    /// The response status code and headers were received.
    /// </summary> 
    ResponseHeader = 2,
    
    /// <summary>
    /// A download progress event was received.
    /// </summary>
    DownloadProgress = 3,
    
    /// <summary>
    /// The full response including the body was received.
    /// </summary>
    Response = 4,
    
    /// <summary>
    /// A custom event from an interceptor or a backend.
    /// </summary>
    User = 5
}