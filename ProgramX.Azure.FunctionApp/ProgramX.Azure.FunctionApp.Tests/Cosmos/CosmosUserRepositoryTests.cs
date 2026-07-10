using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.Cosmos;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using ProgramX.Azure.FunctionApp.Tests.Mocks;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.Cosmos;

[Category("Unit")]
[Category("Cosmos")]
[Category("CosmosUserRepository")]
[TestFixture]
public class CosmosUserRepositoryTests : CosmosTestBase
{
    [Test]
    public async Task GetUsersAsync_WithoutPagedCriteria_ShouldReturnAllUsers()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users);
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetUsersAsync(new GetUsersCriteria());

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(5));
        Assert.That(result.IsRequiredToBeOrderedByClient, Is.False);
    }

    [Test]
    public async Task GetUsersAsync_WithPagedCriteria_ShouldReturnThreeOfFiveUsersOnPageOne()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Take(3)
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetUsersAsync(new GetUsersCriteria(), new PagedCriteria()
        {
            ItemsPerPage = 3,
            Offset = 0
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(3));
        Assert.That(result.IsRequiredToBeOrderedByClient, Is.False);
    }

    [Test]
    public async Task GetUsersAsync_WithPagedCriteria_ShouldReturnThreeOfSixUsersOnPageTwo()
    {
        var users = base.CreateTestUsers(6).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Take(3)
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetUsersAsync(new GetUsersCriteria(), new PagedCriteria()
        {
            ItemsPerPage = 3,
            Offset = 3
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(3));
        Assert.That(result.IsRequiredToBeOrderedByClient, Is.False);
    }

    [Test]
    public void GetUsersInRole_ShouldReturnMatchingUsers()
    {
        var users = base.CreateTestUsers(5).ToList();
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users);
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = target.GetUsersInRole("role 1", users);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public async Task GetUserByIdAsync_WithExistingId_ShouldReturnUser()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q => q.Id == users.First().Id)
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetUserByIdAsync(users.First().Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(users.First().Id));
    }

    [Test]
    public async Task GetUserByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        var nonExistentId = "non-existent";

        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q => q.Id == nonExistentId)
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetUserByIdAsync(nonExistentId);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetUserByUserNameAsync_WithExistingUserName_ShouldReturnUser()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q => q.UserName == users.First().UserName)
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetUserByUserNameAsync(users.First().UserName);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(users.First().Id));
    }

    [Test]
    public async Task GetUserByUserNameAsync_WithNonExistentUserName_ShouldReturnNull()
    {
        var nonExistentUserName = "non-existent";

        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q => q.UserName == nonExistentUserName)
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetUserByUserNameAsync(nonExistentUserName);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void DeleteUserByIdAsync_WithEmptyId_ShouldThrowException()
    {
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>();
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<ArgumentException>(async () => await target.DeleteUserByIdAsync(string.Empty));
    }

    [Test]
    public void DeleteUserByIdAsync_WithErrorResponse_ShouldThrowException()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<UserPassword>()
        {
            ConfigureContainerFunc = (container) =>
            {
                var mockedItemResponse = new Mock<ItemResponse<UserPassword>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.BadRequest);

                container.Setup(q => q.DeleteItemAsync<UserPassword>(It.IsAny<string>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<RepositoryException>(async () => await target.DeleteUserByIdAsync(users.First().Id));
    }

    [Test]
    public async Task DeleteUserByIdAsync_WithExistingId_ShouldSucceed()
    {
        var users = base.CreateTestUsers(5).ToList();
        var userIdToDelete = users.First().Id;

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>()
        {
            MutateItems = (items) => items.Where(q => q.Id != userIdToDelete),
            ConfigureContainerFunc = (container) =>
            {
                var mockedItemResponse = new Mock<ItemResponse<UserPassword>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.NoContent);

                container.Setup(q => q.DeleteItemAsync<UserPassword>(It.IsAny<string>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        await target.DeleteUserByIdAsync(userIdToDelete);
    }

    [Test]
    public void GetUserByIdAsync_WithEmptyId_ShouldThrowException()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q => q.Id == users.First().Id)
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<ArgumentException>(async () => await target.GetUserByIdAsync(string.Empty));
    }
}
