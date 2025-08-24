namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class UpdateResponse
{
    public bool isOk { get; set; }
    public string? errorMessage { get; set; }

    public HttpEventType httpEventType { get; set; }

    public long? bytesTransferred { get; set; } = null;

    public long? totalBytesToTransfer { get; set; } = null;
}