using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Moq;

namespace ProgramX.Azure.FunctionApp.Tests.Mocks;

public class MockedBlobServiceClientFactory
{
    IDictionary<string,IEnumerable<byte>> _blobs = new Dictionary<string, IEnumerable<byte>>();
    
    // /// <summary>
    // /// If the default behaviour is not sufficient, this can be overridden to customise
    // /// for created mock.
    // /// </summary>
    // public Action<Mock<Container>>? ConfigureContainerFunc { get; set; }
    
    // /// <summary>
    // /// Apply a delegate to filter results for the tested subject to pass. Useful
    // /// when performing queries on the mocked CosmosDB.
    // /// </summary>
    // /// <typeparam name="T">Type of item to return.</typeparam>
    // /// <returns>A filtered list of items.</returns>
    // public Func<IEnumerable<T>,IEnumerable<T>>? FilterItems { get; set; }
    //
    // /// <summary>
    // /// Mutate the items returned by the mocked CosmosDB. Useful when performing
    // /// write operations on the mocked CosmosDB.
    // /// </summary>
    // /// <typeparam name="T">Type of item to return.</typeparam>
    // /// <returns>A mutated list of items.</returns>
    // public Func<IEnumerable<T>,IEnumerable<T>>? MutateItems { get; set; }
    
    /// <summary>
    /// Configures the mock to return a mock container with no items.
    /// </summary>
    public MockedBlobServiceClientFactory()
    {
        
    }
    
    /// <summary>
    /// Configures the mock to return a mock container with the specified items.
    /// </summary>
    /// <param name="items">Items to include in the seeded data store.</param>
    public MockedBlobServiceClientFactory(IDictionary<string,IEnumerable<byte>> blobs) : this()
    {
        _blobs = blobs;
    }
    
    
    /// <summary>
    /// Creates the mock Blob Service client.
    /// </summary>
    /// <returns>A mocked Blob Service client.</returns>
    public Mock<BlobServiceClient> Create()
    {
        var mockedBlobService = CreateDefaultMockClient();
        // if (ConfigureContainerFunc != null)
        // {
        //     ConfigureContainerFunc(mockContainer);
        // }
        return mockedBlobService;
    }

    private Mock<BlobServiceClient> CreateDefaultMockClient()
    {
        var mockBlobContainerInfo = new Mock<BlobContainerInfo>();
        
        var mockCreateIfNotExistsAsyncResponse = new Mock<Response<BlobContainerInfo>>();
        mockCreateIfNotExistsAsyncResponse.Setup(x=>x.Value).Returns(mockBlobContainerInfo.Object);
        
        var mockUploadAsyncBlobContentInfo = new Mock<BlobContentInfo>();
        
        var mockUploadAsyncResponse = new Mock<Response<BlobContentInfo>>();
        mockUploadAsyncResponse.Setup(x=>x.Value).Returns(mockUploadAsyncBlobContentInfo.Object);
        
        var mockBlobClient = new Mock<BlobClient>();
        mockBlobClient.Setup(x=>x.UploadAsync(It.IsAny<Stream>(),It.IsAny<BlobUploadOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUploadAsyncResponse.Object);
            
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        
        mockBlobContainerClient.Setup(x=>x.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string,string>?>(), 
                It.IsAny<BlobContainerEncryptionScopeOptions?>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCreateIfNotExistsAsyncResponse.Object);
        mockBlobContainerClient.Setup(x=>x.GetBlobClient(It.IsAny<string>()))
            .Returns(mockBlobClient.Object);
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(mockBlobContainerClient.Object);
        return mockBlobServiceClient;
    }
    
}