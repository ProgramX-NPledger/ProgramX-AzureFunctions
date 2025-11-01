using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Contract;

namespace ProgramX.Azure.FunctionApp.AzureStorage;

public class AzureStorageClient(BlobServiceClient blobServiceClient) : IStorageClient
{
    public async Task<IStorageFolder> GetStorageFolderAsync(string folderName)
    {
        var avatarImagesBlockContainerClient = blobServiceClient.GetBlobContainerClient(folderName);
        await avatarImagesBlockContainerClient.CreateIfNotExistsAsync();

        return new AzureBlobContainerClient(avatarImagesBlockContainerClient);
    }
    
    
}