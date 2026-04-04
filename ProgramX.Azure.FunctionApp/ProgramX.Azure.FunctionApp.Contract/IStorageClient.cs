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

    /// <summary>
    /// Returns the name of a blob.
    /// </summary>
    /// <param name="blobName">The name of the blob required.</param>
    /// <returns>Name of the blob in the storage client.</returns>
    string GetBlobName(BlobNames blobName);

}

public enum BlobNames
{
    AvatarImages
}