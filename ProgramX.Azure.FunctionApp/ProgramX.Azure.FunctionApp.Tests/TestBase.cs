using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Moq;
using NUnit.Framework;
using ProgramX.Azure.FunctionApp.Constants;

namespace ProgramX.Azure.FunctionApp.Tests;

public abstract class TestBase
{
    protected Mock<ILogger> MockLogger { get; private set; } = null!;
    protected Mock<CosmosClient> MockCosmosClient { get; private set; } = null!;
    protected Mock<Container> MockContainer { get; private set; } = null!;
    protected IConfiguration Configuration { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        MockLogger = new Mock<ILogger>();
        MockCosmosClient = new Mock<CosmosClient>();
        MockContainer = new Mock<Container>();
        
        MockCosmosClient
            .Setup(x => x.GetContainer(DataConstants.CoreDatabaseName, DataConstants.UsersContainerName))
            .Returns(MockContainer.Object);

        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();
    }

    [TearDown]
    public virtual void TearDown()
    {
        // Cleanup code if needed
    }
}
