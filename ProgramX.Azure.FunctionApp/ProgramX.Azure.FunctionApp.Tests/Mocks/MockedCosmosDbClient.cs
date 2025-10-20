using Microsoft.Azure.Cosmos;
using Moq;

namespace ProgramX.Azure.FunctionApp.Tests.Mocks;

public class MockedCosmosDbClient
{
    public Mock<CosmosClient> MockedCosmosClient { get; set; }
    public Mock<Database> MockedDatabase { get; set; }
    public Mock<Container> MockedContainer { get; set; }
}