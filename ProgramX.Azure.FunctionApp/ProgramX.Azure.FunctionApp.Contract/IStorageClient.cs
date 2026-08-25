namespace ProgramX.Azure.FunctionApp.Contract;

/// <summary>
/// Provides storage services for files.
/// </summary>
public interface IStorageClient
{
    /// <summary>
    /// Gets a storage folder by name.
    /// </summary>
    /// <param name="folderName">Name of the folder required.</param>
    /// <returns>A <see cref="IStorageFolder"/> that can be used to store and query items.</returns>
    Task<IStorageFolder> GetStorageFolderAsync(string folderName);

    
}

// container name restrictions:
// * Be lowercase
// * Be 3–63 characters
// * Contain only letters, numbers, and hyphens
// * Not start or end with a hyphen
public enum BlobNames
{
    AvatarImages
}