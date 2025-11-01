using System.Net;
using Microsoft.Azure.Cosmos;
using Moq;

namespace ProgramX.Azure.FunctionApp.Tests.Mocks;

/// <summary>
/// Creates a mocked repository client for the type of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Type of item that will be stored.</typeparam>
public class MockedRepositoryFactory<TTypeOfRepository, TRepository> where TRepository : class
{
    readonly List<TTypeOfRepository> _items;
    
    /// <summary>
    /// Apply a delegate to filter results for the tested subject to pass. Useful
    /// when performing queries on the mocked CosmosDB.
    /// </summary>
    /// <typeparam name="T">Type of item to return.</typeparam>
    /// <returns>A filtered list of items.</returns>
    public Func<IEnumerable<TTypeOfRepository>,IEnumerable<TTypeOfRepository>>? FilterItems { get; set; }
    
    /// <summary>
    /// Mutate the items returned by the mocked CosmosDB. Useful when performing
    /// write operations on the mocked CosmosDB.
    /// </summary>
    /// <typeparam name="T">Type of item to return.</typeparam>
    /// <returns>A mutated list of items.</returns>
    public Func<IEnumerable<TTypeOfRepository>,IEnumerable<TTypeOfRepository>>? MutateItems { get; set; }
    
    /// <summary>
    /// Configures the mock to contain no items.
    /// </summary>
    public MockedRepositoryFactory() 
    {
        _items = Enumerable.Empty<TTypeOfRepository>().ToList();
    }
    
    /// <summary>
    /// Configures the mock to contain the specified items.
    /// </summary>
    /// <param name="items">Items to include in the seeded data store.</param>
    public MockedRepositoryFactory(IEnumerable<TTypeOfRepository> items) : this()
    {
        _items = items.ToList();
    }
    
    
    /// <summary>
    /// Creates the mock repository.
    /// </summary>
    /// <returns>A mocked repository.</returns>
    public Mock<TRepository> Create()
    {
        var mockedRepository = new Mock<TRepository>();
        return mockedRepository;
    }

 
}