using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ProgramX.Azure.FunctionApp.Contract;

namespace ProgramX.Azure.FunctionApp.AzureStorage;

public class AzureBlobContainerClient : IStorageFolder
{
    private readonly BlobContainerClient _blobContainerClient;

    public AzureBlobContainerClient(BlobContainerClient blobContainerClient)
    {
        _blobContainerClient = blobContainerClient;
        FolderName = blobContainerClient.Name;
    }

    public string FolderName { get; private set; }

    public async Task<IStorageFolder.SaveFileResult> SaveFileAsync(string fileName, Stream stream, string contentType = "application/octet-stream")
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
        
        var blob = _blobContainerClient.GetBlobClient(fileName);

        var headers = new BlobHttpHeaders
        {
            ContentType = string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType
        };

        var savedFileResult = new IStorageFolder.SaveFileResult();
        
        try
        {
            if (!await _blobContainerClient.ExistsAsync())
            {
                var blobContainerInfo = await _blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                var createIfNotExistsResponse = blobContainerInfo.GetRawResponse();
                savedFileResult.ContainerWasCreated = createIfNotExistsResponse.Status == 201;
            }
        }
        catch (Exception  e)
        {
            Console.WriteLine(e);
            throw;
        }
        
        try
        {
            // Stream directly to Blob Storage (no buffering in memory)
            await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers, Conditions = null });

            savedFileResult.ContentType = contentType;
            savedFileResult.Url = blob.Uri.ToString();
            return savedFileResult;
        }
        catch (RequestFailedException requestFailedException) when (requestFailedException.Status == 409)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            await blob.DeleteIfExistsAsync();

            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = headers
            });

            savedFileResult.OriginalWasOverwritten = true;
            savedFileResult.ContentType = contentType;
            savedFileResult.Url = blob.Uri.ToString();
            return savedFileResult;
        }
        catch (RequestFailedException requestFailedException)
        {
            Console.WriteLine(
                $"Azure Blob Storage request failed. " +
                $"Container: {_blobContainerClient.Name}, " +
                $"Blob: {fileName}, " +
                $"Status: {requestFailedException.Status}, " +
                $"ErrorCode: {requestFailedException.ErrorCode}, " +
                $"Message: {requestFailedException.Message}");
            throw;
        }



    }
    
    public async Task DeleteFileAsync(string fileName)
    {
        var blob = _blobContainerClient.GetBlobClient(fileName);
        await blob.DeleteIfExistsAsync();
    }

    public async Task<StorageFile> GetStorageFileAsync(string fileName)
    {
        // TODO: return null if not found
        var blob = _blobContainerClient.GetBlobClient(fileName);
        var properties = await blob.GetPropertiesAsync();
        var content = await blob.OpenReadAsync();

        return new StorageFile
        {
            Content = content,
            ContentType = properties.Value.ContentType ?? "application/octet-stream",
            FileName = fileName
        };
    }
}
