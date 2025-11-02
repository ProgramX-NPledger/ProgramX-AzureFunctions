using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Azure;
using Moq;
using ProgramX.Azure.FunctionApp.AzureStorage;
using ProgramX.Azure.FunctionApp.Contract;

namespace ProgramX.Azure.FunctionApp.Tests.AzureStorage;

[Category("Unit")]
[Category("Azure")]
[Category("AzureStorageClient")]
[TestFixture]
public class AzureStorageClientTests
{

    [Test]
    public async Task GetStorageFolderAsync_Succeeds()
    {
        var expectedFolderName = "test-folder";
        
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        mockBlobContainerClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(),
            It.IsAny<IDictionary<string,string>>(),
            It.IsAny<BlobContainerEncryptionScopeOptions>(),
            It.IsAny<CancellationToken>()));
        
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(mockBlobContainerClient.Object);

        var target = new AzureStorageClient(mockBlobServiceClient.Object);

        var result = await target.GetStorageFolderAsync(expectedFolderName);
        
        Assert.NotNull(result);
        Assert.IsInstanceOf<IStorageFolder>(result);
        
    }
}