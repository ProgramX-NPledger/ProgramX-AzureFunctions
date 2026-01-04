using Microsoft.Azure.Cosmos;
using ProgramX.Azure.FunctionApp.Cosmos;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Tests.Mocks;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.Cosmos;

[Category("Unit")]
[Category("Cosmos")]
[Category("CosmosReader")]
[TestFixture]
public class CosmosReaderTests : CosmosTestBase
{

    [Test]
    public async Task GetItemsAsync_ShouldReturnSixItems()
    {
        var mockedCosmosDbClientFactory =
            new MockedCosmosDbClientFactory<User>(new List<User>());
        mockedCosmosDbClientFactory.AddItems(CreateTestUsers(6));
        
        var mockedCosmosDbClient = mockedCosmosDbClientFactory.Create();

        var target = new CosmosReader<User>(mockedCosmosDbClient.MockedCosmosClient.Object, "database",
            "container", "partitionKeyPath");
        
        var queryDefinition = new QueryDefinition("SELECT * FROM c");
        var result = await target.GetItemsAsync(queryDefinition);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(6));
    }


    
    
}