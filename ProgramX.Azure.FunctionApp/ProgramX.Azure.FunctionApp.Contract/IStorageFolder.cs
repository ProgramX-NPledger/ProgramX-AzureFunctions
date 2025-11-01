namespace ProgramX.Azure.FunctionApp.Contract;

/// <summary>
/// Provides storage services for files.
/// </summary>
public interface IStorageFolder
{
    /// <summary>
    /// Name of the Storage Folder.
    /// </summary>
    string FolderName { get; }

    /// <summary>
    /// Saves a file to the storage folder.
    /// </summary>
    /// <param name="fileName">Filename of the file.</param>
    /// <param name="stream">Data to be stored.</param>
    /// <param name="contentType">MIME Content Type of the data.</param>
    /// <returns>The URL of the file.</returns>   
    Task<SaveFileResult> SaveFileAsync(string fileName, Stream stream, string contentType = "application/octet-stream");

    /// <summary>
    /// Deletes a file from the storage folder.
    /// </summary>
    /// <param name="fileName">Filename of file to delete.</param>
    Task DeleteFileAsync(string fileName);

    /// <summary>
    /// Response from SaveFileAsync.
    /// </summary>
    struct SaveFileResult
    {
        /// <summary>
        /// The URL to the file.
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// The MIME Content Type of the file.
        /// </summary>
        public string ContentType { get; set; }
    }
}