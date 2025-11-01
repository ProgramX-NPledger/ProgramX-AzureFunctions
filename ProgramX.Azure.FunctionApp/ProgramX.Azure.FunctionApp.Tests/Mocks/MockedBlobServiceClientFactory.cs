using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;

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
    public Mock<IStorageClient> Create()
    {
        var mockedBlobService = CreateDefaultMockClient();
        // if (ConfigureContainerFunc != null)
        // {
        //     ConfigureContainerFunc(mockContainer);
        // }
        return mockedBlobService;
    }

    private Mock<IStorageClient> CreateDefaultMockClient()
    {
        var mockStoageClient = new Mock<IStorageClient>();
        return mockStoageClient;
        
    }
    
}