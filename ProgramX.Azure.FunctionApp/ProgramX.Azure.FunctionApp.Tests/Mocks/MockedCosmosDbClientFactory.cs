using System.Net;
using Microsoft.Azure.Cosmos;
using Moq;

namespace ProgramX.Azure.FunctionApp.Tests.Mocks;

/// <summary>
/// Creates a mocked CosmosDB client for the type of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Type of item that will be stored.</typeparam>
public class MockedCosmosDbClientFactory<T>
{
    List<T> _items = Enumerable.Empty<T>().ToList();
    
    /// <summary>
    /// If the default behaviour is not sufficient, this can be overridden to customise
    /// for created mock.
    /// </summary>
    public Action<Mock<Container>>? ConfigureContainerFunc { get; set; }
    
    /// <summary>
    /// Apply a delegate to filter results for the tested subject to pass. Useful
    /// when performing queries on the mocked CosmosDB.
    /// </summary>
    /// <typeparam name="T">Type of item to return.</typeparam>
    /// <returns>A filtered list of items.</returns>
    public Func<IEnumerable<T>,IEnumerable<T>>? FilterItems { get; set; }
    
    /// <summary>
    /// Mutate the items returned by the mocked CosmosDB. Useful when performing
    /// write operations on the mocked CosmosDB.
    /// </summary>
    /// <typeparam name="T">Type of item to return.</typeparam>
    /// <returns>A mutated list of items.</returns>
    public Func<IEnumerable<T>,IEnumerable<T>>? MutateItems { get; set; }
    
    /// <summary>
    /// Configures the mock to return a mock container with no items.
    /// </summary>
    public MockedCosmosDbClientFactory()
    {
        
    }
    
    /// <summary>
    /// Configures the mock to return a mock container with the specified items.
    /// </summary>
    /// <param name="items">Items to include in the seeded data store.</param>
    public MockedCosmosDbClientFactory(IEnumerable<T> items) : this()
    {
        _items = items.ToList();
    }

    /// <summary>
    /// Adds items to the mock container.
    /// </summary>
    /// <param name="items">List of items</param>
    public void AddItems(IEnumerable<T> items)
    {
        _items.AddRange(items);
    }

    
    /// <summary>
    /// Creates the mock CosmosDB client.
    /// </summary>
    /// <returns>A mocked CosmosDB client.</returns>
    public MockedCosmosDbClient Create()
    {
        var mockContainer = CreateDefaultMockContainer();
        if (ConfigureContainerFunc != null)
        {
            ConfigureContainerFunc(mockContainer);
        }
        var mockDatabase = CreateDefaultMockDatabase(mockContainer);
        var mockCosmosClient = CreateDefaultMockClient(mockDatabase, mockContainer);

        var mockedCosmosDbClient = new MockedCosmosDbClient()
        {
            MockedContainer = mockContainer,
            MockedDatabase = mockDatabase,
            MockedCosmosClient = mockCosmosClient,
        };
        return mockedCosmosDbClient;
    }

    private Mock<CosmosClient> CreateDefaultMockClient(Mock<Database> mockDatabase, Mock<Container> mockContainer)
    {
        var mockDatabaseResponse = new Mock<DatabaseResponse>();
        mockDatabaseResponse.Setup(x=>x.StatusCode).Returns(System.Net.HttpStatusCode.Created);
        mockDatabaseResponse.Setup(x => x.Database).Returns(mockDatabase.Object);
        
        var mockCosmosClient = new Mock<CosmosClient>();
        mockCosmosClient.Setup(x => x.CreateDatabaseIfNotExistsAsync(
                It.IsAny<string>(),
                It.IsAny<ThroughputProperties>(),
                It.IsAny<RequestOptions>(),
                It.IsNotNull<CancellationToken>()))
            .ReturnsAsync(mockDatabaseResponse.Object);
        mockCosmosClient.Setup(x => x.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockContainer.Object);
        mockCosmosClient.Setup(x => x.GetDatabase(It.IsAny<string>()))
            .Returns(mockDatabase.Object);
        
        return mockCosmosClient;
    }

    private Mock<Database> CreateDefaultMockDatabase(Mock<Container> mockContainer)
    {
        var mockDatabase = new Mock<Database>();
        mockDatabase.Setup(x => x.CreateContainerIfNotExistsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateContainerResponse(mockContainer.Object).Object);
        mockDatabase.Setup(x => x.GetContainer(It.IsAny<string>()))
            .Returns(mockContainer.Object);
        return mockDatabase;
    }


    private Mock<Container> CreateDefaultMockContainer()
    {
        var mockFeedResponseOfT = new Mock<FeedResponse<T>>();
        mockFeedResponseOfT.Setup(x => x.Count).Returns(_items.Count);
        if (FilterItems != null)
        {
            mockFeedResponseOfT.Setup(x => x.GetEnumerator())
                .Returns(() => FilterItems(_items).GetEnumerator());
        }
        else
        {
            mockFeedResponseOfT.Setup(x => x.GetEnumerator())
                .Returns(() => _items.GetEnumerator());
        }

        // Setup the FeedIterator to return the items in the list
        var mockFeedIteratorOfT = new Mock<FeedIterator<T>>();
        var setupSequence = mockFeedIteratorOfT.SetupSequence(x => x.HasMoreResults);
        for (int i = 0; i < _items.Count; i++)
        {
            setupSequence = setupSequence.Returns(true);
        }
        setupSequence = setupSequence.Returns(false);
        
        mockFeedIteratorOfT.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFeedResponseOfT.Object);
        
        var mockFeedResponseOfInt = new Mock<FeedResponse<int>>();
        mockFeedResponseOfInt.Setup(x=>x.Count).Returns(1); 
        mockFeedResponseOfInt.Setup(x=> x.GetEnumerator())
            .Returns(new List<int>() { 1 }.GetEnumerator());
        
        var mockFeedIteratorOfInt = new Mock<FeedIterator<int>>();
        mockFeedIteratorOfInt.SetupSequence(x => x.HasMoreResults)
            .Returns(true)
            .Returns(false);
        mockFeedIteratorOfInt.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFeedResponseOfInt.Object);
        mockFeedIteratorOfInt.Setup(x=>x.ReadNextAsync(CancellationToken.None))
            .ReturnsAsync(mockFeedResponseOfInt.Object);
        
        var mockContainer = new Mock<Container>();
        
        mockContainer.Setup(x =>
                x.GetItemQueryIterator<T>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
            .Returns(mockFeedIteratorOfT.Object);
        mockContainer.Setup(x =>
                x.GetItemQueryIterator<int>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
            .Returns(mockFeedIteratorOfInt.Object);
        
        if (MutateItems != null)
        {
            // set-up all the methods that mutate the items
            mockContainer.Setup(x => x.DeleteItemStreamAsync(It.IsAny<string>(), It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    MutateItems.Invoke(_items);
                })
                .ReturnsAsync(new ResponseMessage(System.Net.HttpStatusCode.OK));
            mockContainer.Setup(x => x.UpsertItemAsync(It.IsAny<string>(), It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    MutateItems.Invoke(_items);
                })
                .ReturnsAsync(CreateItemResponse<string>());
            mockContainer.Setup(x => x.ReplaceItemAsync(It.IsAny<T>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    MutateItems.Invoke(_items);
                })
                .ReturnsAsync(CreateItemResponse<T>());
            mockContainer.Setup(x => x.CreateItemAsync(It.IsAny<T>(),  It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    MutateItems.Invoke(_items);
                })
                .ReturnsAsync(CreateItemResponse<T>());
        }
        
        return mockContainer;
        
      
    }


    
    private ItemResponse<TResponseType> CreateItemResponse<TResponseType>(HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        var mockResponse = new Mock<ItemResponse<TResponseType>>();
        mockResponse.Setup(x => x.StatusCode).Returns(statusCode);
        return mockResponse.Object;
    }


    private Mock<ContainerResponse> CreateContainerResponse(Container returnsContainer)
    {
        var mockContainerResponse = new Mock<ContainerResponse>();
        mockContainerResponse.Setup(x => x.Container).Returns(returnsContainer);
        return mockContainerResponse;
    }

 
}