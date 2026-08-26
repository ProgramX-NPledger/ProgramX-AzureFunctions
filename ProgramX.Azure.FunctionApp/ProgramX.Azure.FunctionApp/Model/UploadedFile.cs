namespace ProgramX.Azure.FunctionApp.Model;

public class UploadedFile
{
    public string OriginalFileName { get; set; }
    public string ContentType { get; set; }
    public byte[] Data { get; set; }
    
    public string FilePurpose { get; set; }
}