namespace ProgramX.Azure.FunctionApp.Model;

public class SavedFile
{
    public string FileName { get; set; }
    public SavedFileStatus Status { get; set; } = SavedFileStatus.IsProcessing;

    public long FileSize { get; set; }
    
}

public enum SavedFileStatus
{
    Ok,
    IsProcessing,
    InvalidBase64
}