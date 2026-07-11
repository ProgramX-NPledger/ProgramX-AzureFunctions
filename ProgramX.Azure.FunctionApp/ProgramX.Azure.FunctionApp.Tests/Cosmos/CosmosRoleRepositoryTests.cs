using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.Cosmos;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using ProgramX.Azure.FunctionApp.Tests.Mocks;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.Cosmos;

[Category("Unit")]
[Category("Cosmos")]
[Category("CosmosRoleRepository")]
[TestFixture]
public class CosmosRoleRepositoryTests : CosmosTestBase
{
    private static Role CreateTestRole(string roleName = "test-role", string? description = "Test role description")
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            RoleName = roleName,
            Description = description,
            SchemaVersionNumber = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // GetRolesAsync passes ContainerProperties to CosmosReader, which then calls
    // CreateContainerIfNotExistsAsync(ContainerProperties, ThroughputProperties, ...).
    // MockedCosmosDbClientFactory only mocks the string-based overload, so we add this setup.
    private static void SetupContainerPropertiesOverload(MockedCosmosDbClient mockCosmosClient)
    {
        var mockContainerResponse = new Mock<ContainerResponse>();
        mockContainerResponse.Setup(x => x.Container).Returns(mockCosmosClient.MockedContainer.Object);
        mockCosmosClient.MockedDatabase
            .Setup(x => x.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(),
                It.IsAny<ThroughputProperties>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContainerResponse.Object);
    }

    private static void SetupRoleReplace(Mock<Container> container, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        var mockItemResponse = new Mock<ItemResponse<Role>>();
        mockItemResponse.SetupGet(x => x.StatusCode).Returns(statusCode);
        container.Setup(q => q.ReplaceItemAsync(
                It.IsAny<Role>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockItemResponse.Object);
    }

    
    private static void SetupUserQueryIterator(Mock<Container> container, IEnumerable<User> users)
    {
        var userList = users.ToList();
        var feedResponse = new Mock<FeedResponse<User>>();
        feedResponse.Setup(x => x.GetEnumerator()).Returns(() => userList.GetEnumerator());

        var feedIterator = new Mock<FeedIterator<User>>();
        feedIterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedResponse.Object);

        container.Setup(x => x.GetItemQueryIterator<User>(
                It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Returns(feedIterator.Object);
    }

    private static void SetupUserReplace(Mock<Container> container, System.Net.HttpStatusCode statusCode)
    {
        var mockItemResponse = new Mock<ItemResponse<User>>();
        mockItemResponse.SetupGet(x => x.StatusCode).Returns(statusCode);
        container.Setup(q => q.ReplaceItemAsync(
                It.IsAny<User>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockItemResponse.Object);
    }

    [Test]
    public void UpdateRoleAsync_WithEmptyRoleName_ShouldThrowArgumentException()
    {
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<Role>();
        var mockCosmosClient = mockCosmosClientFactory.Create();
        var mockLogger = new Mock<ILogger<CosmosRoleRepository>>();

        var target = new CosmosRoleRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await target.UpdateRoleAsync(string.Empty, "description", null));
    }

    [Test]
    public void UpdateRoleAsync_WhenRoleNotFound_ShouldThrowItemNotFoundException()
    {
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<Role>()
        {
            FilterItems = items => items.Where(_ => false)
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();
        SetupContainerPropertiesOverload(mockCosmosClient);
        var mockLogger = new Mock<ILogger<CosmosRoleRepository>>();

        var target = new CosmosRoleRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);

        Assert.ThrowsAsync<ItemNotFoundException>(async () =>
            await target.UpdateRoleAsync("non-existent-role", "description", null));
    }

    [Test]
    public async Task UpdateRoleAsync_WithNullUsersInRole_ShouldUpdateDescriptionAndReturnRole()
    {
        var role = CreateTestRole();
        const string newDescription = "Updated description";

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<Role>([role])
        {
            FilterItems = items => items.Where(r => r.RoleName == role.RoleName),
            ConfigureContainerFunc = container => SetupRoleReplace(container)
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();
        SetupContainerPropertiesOverload(mockCosmosClient);
        var mockLogger = new Mock<ILogger<CosmosRoleRepository>>();

        var target = new CosmosRoleRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.UpdateRoleAsync(role.RoleName, newDescription, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.RoleName, Is.EqualTo(role.RoleName));
        Assert.That(result.Description, Is.EqualTo(newDescription));
    }

    [Test]
    public void UpdateRoleAsync_WhenReplaceItemFails_ShouldThrowItemUpdateException()
    {
        var role = CreateTestRole();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<Role>([role])
        {
            FilterItems = items => items.Where(r => r.RoleName == role.RoleName),
            ConfigureContainerFunc = container => SetupRoleReplace(container, System.Net.HttpStatusCode.BadRequest)
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();
        SetupContainerPropertiesOverload(mockCosmosClient);
        var mockLogger = new Mock<ILogger<CosmosRoleRepository>>();

        var target = new CosmosRoleRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);

        Assert.ThrowsAsync<ItemUpdateException>(async () =>
            await target.UpdateRoleAsync(role.RoleName, "new description", null));
    }

    [Test]
    public async Task UpdateRoleAsync_WithUsersInRole_WhenNoUsersToModify_ShouldSucceed()
    {
        var role = CreateTestRole();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<Role>([role])
        {
            FilterItems = items => items.Where(r => r.RoleName == role.RoleName),
            ConfigureContainerFunc = container =>
            {
                SetupRoleReplace(container);
                SetupUserQueryIterator(container, []);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();
        SetupContainerPropertiesOverload(mockCosmosClient);
        var mockLogger = new Mock<ILogger<CosmosRoleRepository>>();

        var target = new CosmosRoleRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.UpdateRoleAsync(role.RoleName, "updated", ["user1"]);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.RoleName, Is.EqualTo(role.RoleName));
    }

    [Test]
    public void UpdateRoleAsync_WithUsersInRole_WhenUserUpdateFails_ShouldThrowItemUpdateException()
    {
        var role = CreateTestRole();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "user1",
            EmailAddress = "user1@example.com",
            Roles = []
        };

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<Role>([role])
        {
            FilterItems = items => items.Where(r => r.RoleName == role.RoleName),
            ConfigureContainerFunc = container =>
            {
                SetupRoleReplace(container);
                SetupUserQueryIterator(container, [user]);
                SetupUserReplace(container, System.Net.HttpStatusCode.BadRequest);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();
        SetupContainerPropertiesOverload(mockCosmosClient);
        var mockLogger = new Mock<ILogger<CosmosRoleRepository>>();

        var target = new CosmosRoleRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);

        Assert.ThrowsAsync<ItemUpdateException>(async () =>
            await target.UpdateRoleAsync(role.RoleName, "updated", ["user1"]));
    }
    
    
    [Test]
    public async Task UpdateRoleAsync_WithUsersInRole_WhenAddedUser_ShouldSucceed()
    {
        var role = CreateTestRole();
        var existingUsers = CreateTestUsers(1);
        
        var mockCosmosDbClientFactoryForRole = new MockedCosmosDbClientFactory<Role>([role])
        {
            FilterItems = items => items.Where(r => r.RoleName == role.RoleName),
            ConfigureContainerFunc = container =>
            {
                SetupRoleReplace(container);
                SetupUserReplace(container, HttpStatusCode.OK);
                // users assigned to Role ALREADY
                SetupUserQueryIterator(container, existingUsers);
            }
        };
        var mockCosmosClientForRole = mockCosmosDbClientFactoryForRole.Create();
        SetupContainerPropertiesOverload(mockCosmosClientForRole);
        
        
        var mockLogger = new Mock<ILogger<CosmosRoleRepository>>();

        var target = new CosmosRoleRepository(mockCosmosClientForRole.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.UpdateRoleAsync(role.RoleName, "updated", ["user1", "newUser"]);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.RoleName, Is.EqualTo(role.RoleName));
    }
    
    
    [Test]
    public async Task UpdateRoleAsync_WithUsersInRole_WhenRemovedUser_ShouldSucceed()
    {
        var role = CreateTestRole();
        var existingUsers = CreateTestUsers(1);

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<Role>([role])
        {
            FilterItems = items => items.Where(r => r.RoleName == role.RoleName),
            ConfigureContainerFunc = container =>
            {
                SetupRoleReplace(container);
                SetupUserReplace(container, HttpStatusCode.OK);
                // users assigned to Role ALREADY
                SetupUserQueryIterator(container, existingUsers);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();
        SetupContainerPropertiesOverload(mockCosmosClient);
        
        var mockLogger = new Mock<ILogger<CosmosRoleRepository>>();

        var target = new CosmosRoleRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.UpdateRoleAsync(role.RoleName, "updated", []);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.RoleName, Is.EqualTo(role.RoleName));
    }
}
