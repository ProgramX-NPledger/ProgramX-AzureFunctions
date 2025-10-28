using Microsoft.Azure.Cosmos;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Helpers;

public class CosmosDBRolesProvider : IRolesProvider
{
    private readonly CosmosClient _cosmosClient;

    public CosmosDBRolesProvider(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }
    
    public async Task<IEnumerable<Role>> GetRolesAsync()
    {
        if (_cosmosClient == null) throw new InvalidOperationException("CosmosDB client is not set");
        
        var rolesCosmosDbReader =
            new PagedCosmosDbReader<Role>(_cosmosClient, 
                DataConstants.CoreDatabaseName, 
                DataConstants.UsersContainerName, 
                DataConstants.UserNamePartitionKeyPath);
            
        var queryDefinition= new QueryDefinition("SELECT r.name, r.description, r.applications FROM c JOIN r IN c.roles");
        var roles = await rolesCosmosDbReader.GetNextItemsAsync(queryDefinition,null,null);

        return roles.Items;
        
    }
}