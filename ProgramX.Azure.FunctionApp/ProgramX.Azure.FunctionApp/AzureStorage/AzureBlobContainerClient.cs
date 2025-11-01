using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ProgramX.Azure.FunctionApp.Contract;

namespace ProgramX.Azure.FunctionApp.AzureStorage;

public class AzureBlobContainerClient(BlobContainerClient blobContainerClient) : IStorageFolder
{
    public async Task<IStorageFolder.SaveFileResult> SaveFileAsync(string fileName, Stream stream, string contentType = "application/octet-stream")
    {
        var blob = blobContainerClient.GetBlobClient(fileName);

        var headers = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        // Stream directly to Blob Storage (no buffering in memory)
        await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers });

        return new IStorageFolder.SaveFileResult()
        {
            ContentType = contentType,
            Url = blob.Uri.ToString()
        };
    }
    
    public async Task DeleteFileAsync(string fileName)
    {
        var blob = blobContainerClient.GetBlobClient(fileName);
        await blob.DeleteIfExistsAsync();
    }
}