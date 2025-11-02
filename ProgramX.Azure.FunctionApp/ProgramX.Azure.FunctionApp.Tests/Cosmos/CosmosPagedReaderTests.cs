using Microsoft.Azure.Cosmos;
using ProgramX.Azure.FunctionApp.Cosmos;
using ProgramX.Azure.FunctionApp.Tests.Mocks;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.Cosmos;

[Category("Unit")]
[Category("Cosmos")]
[Category("CosmosPagedReader")]
[TestFixture]
public class CosmosPagedReaderTests : CosmosTestBase
{

    [Test]
    public async Task GetNextItemsAsync_WithNullContinuationTokenAndNoItemsPerPage_ShouldReturnSixItems()
    {
        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User>());
        mockedCosmosDbClientFactory.AddItems(CreateTestUsers(6));
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();

        var target = new CosmosPagedReader<User>(mockedCosmosDbClient.MockedCosmosClient.Object, "database",
            "container", "partitionKeyPath");
        
        var queryDefinition = new QueryDefinition("SELECT * FROM c");
        var result = await target.GetNextItemsAsync(queryDefinition, null);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(6));
    }


    [Test]
    public async Task GetNextItemsAsync_WithNullContinuationTokenAndItemsPerPage_ShouldReturnThreeItems()
    {
        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User>())
            {
                FilterItems = (items) => items.Take(3)
            };
        mockedCosmosDbClientFactory.AddItems(CreateTestUsers(6));

        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();
        

        var target = new CosmosPagedReader<User>(mockedCosmosDbClient.MockedCosmosClient.Object, "database",
            "container", "partitionKeyPath");
        
        var queryDefinition = new QueryDefinition("SELECT * FROM c");
        var result = await target.GetNextItemsAsync(queryDefinition, null,3);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task GetNextsItemAsync_WithContinuationToken_ShouldReturnNextThreeItems()
    {
        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User>())
            {
                FilterItems = (items) => items.Skip(3).Take(3)
            };
        
        mockedCosmosDbClientFactory.AddItems(CreateTestUsers(6));
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();

        var target = new CosmosPagedReader<User>(mockedCosmosDbClient.MockedCosmosClient.Object, "database",
            "container", "partitionKeyPath");
        
        var queryDefinition = new QueryDefinition("SELECT * FROM c");
        var result = await target.GetNextItemsAsync(queryDefinition, null,3);

        var nextResult = await target.GetNextItemsAsync(queryDefinition, result.ContinuationToken,3);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task GetPagedItemsAsync_WithNoOffsetOrItemsPerPage_ShouldReturnFiveItems()
    {
        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User>());
        mockedCosmosDbClientFactory.AddItems(CreateTestUsers(6));
        mockedCosmosDbClientFactory.FilterItems = (items) => items.Take(5);
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();

        var target = new CosmosPagedReader<User>(mockedCosmosDbClient.MockedCosmosClient.Object, "database",
            "container", "partitionKeyPath");
        
        var queryDefinition = new QueryDefinition("SELECT * FROM c");
        var result = await target.GetPagedItemsAsync(queryDefinition);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(5));
    }
    
    
    [Test]
    public async Task GetPagedItemsAsync_WithOffsetNoItemsPerPage_ShouldReturnFiveItems()
    {
        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User>());
        mockedCosmosDbClientFactory.AddItems(CreateTestUsers(6));
        mockedCosmosDbClientFactory.FilterItems = (items) => items.Take(5);
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();

        var target = new CosmosPagedReader<User>(mockedCosmosDbClient.MockedCosmosClient.Object, "database",
            "container", "partitionKeyPath");
        
        var queryDefinition = new QueryDefinition("SELECT * FROM c");
        var result = await target.GetPagedItemsAsync(queryDefinition,1);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(5));
    }
    
    
    [Test]
    public async Task GetPagedItemsAsync_WithOffsetAndItemsPerPage_ShouldReturnThreeItems()
    {
        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User>());
        mockedCosmosDbClientFactory.AddItems(CreateTestUsers(6));
        mockedCosmosDbClientFactory.FilterItems = (items) => items.Take(3);
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();

        var target = new CosmosPagedReader<User>(mockedCosmosDbClient.MockedCosmosClient.Object, "database",
            "container", "partitionKeyPath");
        
        var queryDefinition = new QueryDefinition("SELECT * FROM c");
        var result = await target.GetPagedItemsAsync(queryDefinition,1,3);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(3));
    }
    
    
    [Test]
    public async Task GetOrderedPagedItemsAsync_WithNoOffsetOrItemsPerPage_ShouldReturnFiveItems()
    {
        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User>());
        mockedCosmosDbClientFactory.AddItems(CreateTestUsers(6));
        mockedCosmosDbClientFactory.FilterItems = (items) => items.Take(5);
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();

        var target = new CosmosPagedReader<User>(mockedCosmosDbClient.MockedCosmosClient.Object, "database",
            "container", "partitionKeyPath");
        
        var queryDefinition = new QueryDefinition("SELECT * FROM c");
        var result = await target.GetOrderedPagedItemsAsync(queryDefinition,"field");
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(5));
    }
    
    
    [Test]
    public async Task GetOrderedPagedItemsAsync_WithOffsetNoItemsPerPage_ShouldReturnFiveItems()
    {
        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User>());
        mockedCosmosDbClientFactory.AddItems(CreateTestUsers(6));
        mockedCosmosDbClientFactory.FilterItems = (items) => items.Take(5);
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();

        var target = new CosmosPagedReader<User>(mockedCosmosDbClient.MockedCosmosClient.Object, "database",
            "container", "partitionKeyPath");
        
        var queryDefinition = new QueryDefinition("SELECT * FROM c");
        var result = await target.GetOrderedPagedItemsAsync(queryDefinition,"field",1);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(5));
    }
    
    
    [Test]
    public async Task GetOrderedPagedItemsAsync_WithOffsetAndItemsPerPage_ShouldReturnThreeItems()
    {
        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User>());
        mockedCosmosDbClientFactory.AddItems(CreateTestUsers(6));
        mockedCosmosDbClientFactory.FilterItems = (items) => items.Take(3);
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();

        var target = new CosmosPagedReader<User>(mockedCosmosDbClient.MockedCosmosClient.Object, "database",
            "container", "partitionKeyPath");
        
        var queryDefinition = new QueryDefinition("SELECT * FROM c");
        var result = await target.GetOrderedPagedItemsAsync(queryDefinition,"field",1,3);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(3));
    }

    private IEnumerable<User> CreateTestUsers(int numberOfItems)
    {
        List<User> users = new();
        for (int i = 1; i <= numberOfItems; i++)
        {
            users.Add(new User()
            {
                id = Guid.NewGuid().ToString(),
                userName = $"user{i}",
                emailAddress = $"",
                createdAt = DateTime.UtcNow,
                passwordHash = new byte[] { },
                passwordSalt = new byte[] { },
                firstName = $"First Name {i}",
                lastLoginAt = DateTime.UtcNow,
                lastName = $"Last Name {i}",
                updatedAt = DateTime.UtcNow,
                
            });
        }

        return users;
    }
    
    
}