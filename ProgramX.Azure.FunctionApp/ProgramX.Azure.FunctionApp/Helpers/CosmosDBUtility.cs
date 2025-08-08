using Microsoft.Azure.Cosmos;

namespace ProgramX.Azure.FunctionApp.Helpers;

public static class CosmosDBUtility
{
    public static IEnumerable<T> GetItems<T>(CosmosClient client, string databaseName, string containerName, QueryDefinition queryDefinition)
    {
        List<T> items = new List<T>();
        var itemIterator = client.GetContainer(databaseName,containerName).GetItemQueryIterator<T>(queryDefinition);
        while (itemIterator.HasMoreResults)
        {
            items.AddRange(itemIterator.ReadNextAsync().Result);
        }
        
        return items;
    }
}