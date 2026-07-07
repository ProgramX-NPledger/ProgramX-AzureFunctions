namespace ProgramX.Azure.FunctionApp.Contract;

/// <summary>
/// Response from GetStorageFileAsync.
/// </summary>
public sealed class StorageFile
{
    /// <summary>
    /// The file content stream. Caller is responsible for disposing it.
    /// </summary>
    public required Stream Content { get; init; }

    /// <summary>
    /// The MIME Content Type of the file.
    /// </summary>
    public string ContentType { get; init; } = "application/octet-stream";

    /// <summary>
    /// The original file name.
    /// </summary>
    public string? FileName { get; init; }
}
