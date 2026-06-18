using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
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
    public async Task GetRolesAsync_WithoutPagedCriteria_ShouldReturnAllRoles()
    {
        var roles = base.CreateTestUsers(5)
            .SelectMany(q => q.Roles)
            .DistinctBy(d => d.name).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<Role>(roles);
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetRolesAsync(new GetRolesCriteria());

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(5));
        Assert.That(result.IsRequiredToBeOrderedByClient, Is.True);
    }

    [Test]
    public async Task GetRolesAsync_WithPagedCriteria_ShouldReturnThreeOfSixRolesOnPageOne()
    {
        var roles = base.CreateTestUsers(5)
            .SelectMany(q => q.Roles)
            .DistinctBy(d => d.name).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<Role>(roles)
        {
            FilterItems = (items) => items.Take(3)
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetRolesAsync(new GetRolesCriteria(), new PagedCriteria()
        {
            ItemsPerPage = 3,
            Offset = 0,
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(3));
        Assert.That(result.IsRequiredToBeOrderedByClient, Is.True);
    }

    [Test]
    public async Task GetRolesAsync_WithPagedCriteria_ShouldReturnThreeOfSixRolesOnPageTwo()
    {
        var roles = base.CreateTestUsers(6)
            .SelectMany(q => q.Roles)
            .DistinctBy(d => d.name).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<Role>(roles)
        {
            FilterItems = (items) => items.Skip(3).Take(3)
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetRolesAsync(new GetRolesCriteria(), new PagedCriteria()
        {
            ItemsPerPage = 3,
            Offset = 3,
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(3));
        Assert.That(result.IsRequiredToBeOrderedByClient, Is.True);
    }

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
    public async Task GetUsersAsync_WithPagedCriteria_ShouldReturnThreeOfSixUsersOnPageOne()
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
    public async Task GetApplicationsAsync_WithoutPagedCriteria_ShouldReturnAllApplications()
    {
        var applications = base.CreateTestUsers(5)
            .SelectMany(q => q.Roles)
            .SelectMany(q => q.applications)
            .DistinctBy(d => d.name).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<Application>(applications);
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetApplicationsAsync(new GetApplicationsCriteria());

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(5));
        Assert.That(result.IsRequiredToBeOrderedByClient, Is.False);
    }
    
    [Test]
    public async Task GetApplicationsAsync_WithPagedCriteria_ShouldReturnThreeOfSixRolesOnPageOne()
    {
        var applications = base.CreateTestUsers(5)
            .SelectMany(q => q.Roles)
            .SelectMany(q => q.applications)
            .DistinctBy(d => d.name).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<Application>(applications)
        {
            FilterItems = (items) => items.Take(3)       
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetApplicationsAsync(new GetApplicationsCriteria(),new PagedCriteria()
            {
                ItemsPerPage = 3,
                Offset = 0,
            }
        );

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(3));
        Assert.That(result.IsRequiredToBeOrderedByClient, Is.False);    
    }
    
    [Test]
    public async Task GetApplicationsAsync_WithPagedCriteria_ShouldReturnThreeOfSixRolesOnPageTwo()
    {
        var applications = base.CreateTestUsers(6)
            .SelectMany(q => q.Roles)
            .SelectMany(q => q.applications)
            .DistinctBy(d => d.name).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<Application>(applications)
        {
            FilterItems = (items) => items.Skip(3).Take(3)       
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetApplicationsAsync(new GetApplicationsCriteria(),new PagedCriteria()
            {
                ItemsPerPage = 3,
                Offset = 3,
            }
        );

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.EqualTo(3));
        Assert.That(result.IsRequiredToBeOrderedByClient, Is.False);
    }
    
    [Test]
    public void GetUsersInRole_ShouldReturnTwoOfFour()
    {
        var users = base.CreateTestUsers(5).ToList();
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users);
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = target.GetUsersInRole("role 1",users);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);;
        Assert.That(result.Count, Is.EqualTo(5));
    }
    
    [Test]
    public async Task GetUserByIdAsync_WithExistingId_ShouldReturnUser()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q=>q.Id == users.First().Id)      
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
            FilterItems = (items) => items.Where(q=>q.Id == nonExistentId)      
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetUserByIdAsync(nonExistentId);

        Assert.That(result, Is.Null);
    }
    
    
    [Test]
    public async Task GetRoleByNameAsync_WithExistingName_ShouldReturnRole()
    {
        var users = base.CreateTestUsers(1).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            ConfigureContainerFunc = (container =>
            {
                var mockFeedResponse = new Mock<FeedResponse<Role>>();
                mockFeedResponse.SetupGet(x => x.Count).Returns(users.First().Roles.Count());
                mockFeedResponse.Setup(x => x.GetEnumerator()).Returns(users.First().Roles.GetEnumerator());
                
                var mockFeedIterator = new Mock<FeedIterator<Role>>();
                mockFeedIterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockFeedResponse.Object);
                
                container.Setup(x => x.GetItemQueryIterator<Role>(It.IsAny<QueryDefinition>(), It.IsAny<string>(),
                        It.IsAny<QueryRequestOptions>()))
                    .Returns(mockFeedIterator.Object);
            }),
            FilterItems = (items) => items.Where(q=>q.Roles.First().name == users.First().Roles.First().name)            
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetRoleByNameAsync(users.First().Roles.First().name);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.name, Is.EqualTo(users.First().Roles.First().name));
    }
    
    
    [Test]
    public async Task GetRoleByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        var nonExistentId = "non-existent";

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>()
        {
            ConfigureContainerFunc = (container =>
            {
                var mockFeedResponse = new Mock<FeedResponse<Role>>();
                mockFeedResponse.SetupGet(x => x.Count).Returns(0);
                mockFeedResponse.Setup(x => x.GetEnumerator()).Returns(Enumerable.Empty<Role>().GetEnumerator());
                
                var mockFeedIterator = new Mock<FeedIterator<Role>>();
                mockFeedIterator.SetupGet(x => x.HasMoreResults).Returns(false);
                mockFeedIterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockFeedResponse.Object);
                
                container.Setup(x => x.GetItemQueryIterator<Role>(It.IsAny<QueryDefinition>(), It.IsAny<string>(),
                        It.IsAny<QueryRequestOptions>()))
                    .Returns(mockFeedIterator.Object);
            })
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetRoleByNameAsync(nonExistentId);

        Assert.That(result, Is.Null);
    }

    
    [Test]
    public async Task GetUserByUserNameAsync_WithExistingUserName_ShouldReturnUser()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q=>q.UserName == users.First().UserName)      
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetUserByIdAsync(users.First().UserName);

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
            FilterItems = (items) => items.Where(q=>q.UserName == nonExistentUserName)      
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
            MutateItems = (items) => items.Where(q=>q.Id != userIdToDelete),
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
    public void DeleteRoleByRoleNameAsync_WithErrorResponse_ShouldThrowException()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            ConfigureContainerFunc = (container) =>
            {
                var mockedItemResponse = new Mock<ItemResponse<User>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.BadRequest);

                container.Setup(x => x.ReplaceItemAsync<User>(It.IsAny<User>(), It.IsAny<string>(),
                        It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<RepositoryException>(async () => await target.DeleteRoleByNameAsync(users.First().Roles.First().name));
    }
    
    [Test]
    public async Task DeleteRoleByRoleNameAsync_WithExistingId_ShouldSucceed()
    {
        var users = base.CreateTestUsers(5).ToList();
        var roleNameToDelete = users.First().Roles.First().name;
        
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>()
        {
            MutateItems = (items) => items.Where(q=>q.Id != roleNameToDelete),
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
        await target.DeleteUserByIdAsync(roleNameToDelete);

    }
    
    [Test]
    public async Task GetUserById_WithEmptyId_ShouldThrowException()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q=>q.Id == users.First().Id)      
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<ArgumentException>(async() => await target.GetUserByIdAsync(string.Empty));
             
    }
    
    [Test]
    public async Task GetInsecureUserById_WithExistingId_ShouldReturnUser()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q=>q.Id == users.First().Id)      
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetUserByIdAsync(users.First().Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(users.First().Id));
    }
    
    [Test]
    public async Task GetInsecureUserById_WithNonExistentId_ShouldReturnNull()
    {
        var nonExistentId = "non-existent";
        
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q=>q.Id == nonExistentId)      
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetUserByIdAsync(nonExistentId);

        Assert.That(result, Is.Null);
    }
    
    [Test]
    public async Task UpdateUserAsync_WithValidSecureUser_ShouldSucceed()
    {
        var users = base.CreateTestUsers(5).ToList();
        var userIdToUpdate = users.First().Id;
        
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>()
        {
            MutateItems = (items) => items.Where(q=>q.Id == userIdToUpdate),
            ConfigureContainerFunc = (container) =>
            {
                var mockedItemResponse = new Mock<ItemResponse<User>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.OK);

                container.Setup(q => q.ReplaceItemAsync<User>(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        await target.UpdateUserAsync(users.Single(q=>q.Id==userIdToUpdate));
    }
    
    [Test]
    public async Task UpdateUserAsync_WithErrorResponse_ShouldThrowException()
    {
        var users = base.CreateTestUsers(5).ToList();
        var userIdToUpdate = users.First().Id;
        
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>()
        {
            MutateItems = (items) => items.Where(q=>q.Id == userIdToUpdate),
            ConfigureContainerFunc = (container) =>
            {
                var mockedItemResponse = new Mock<ItemResponse<User>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.BadRequest);

                container.Setup(q => q.ReplaceItemAsync<User>(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<RepositoryException>(async () =>
            await target.UpdateUserAsync(users.Single(q => q.Id == userIdToUpdate)));
    }
    
    [Test]
    public async Task UpdateUserAsync_WithNonExistentUser_ShouldThrowException()
    {
        var users = base.CreateTestUsers(5).ToList();
        var userIdToUpdate = users.First().Id;
        
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>()
        {
            MutateItems = (items) => items.Where(q=>q.Id == userIdToUpdate),
            ConfigureContainerFunc = (container) =>
            {
                var mockedItemResponse = new Mock<ItemResponse<User>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.NotFound);

                container.Setup(q => q.ReplaceItemAsync<User>(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<RepositoryException>(async () =>
            await target.UpdateUserAsync(users.Single(q => q.Id == userIdToUpdate)));
    }
    
    
    [Test]
    public async Task UpdateRoleAsync_WithValidRole_ShouldSucceed()
    {
        var users = base.CreateTestUsers(1).ToList();
        var roleNameToUpdate = users.First().Roles.First().name;
        
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            MutateItems = (items) => items.Where(q=>q.Roles.Any(qq => qq.name==roleNameToUpdate)),
            ConfigureContainerFunc = (container) =>
            {
                var mockFeedResponse = new Mock<FeedResponse<Role>>();
                mockFeedResponse.SetupGet(x => x.Count).Returns(users.Count);
                mockFeedResponse.Setup(x => x.GetEnumerator()).Returns(Enumerable.Empty<Role>().GetEnumerator());
                
                var mockFeedIterator = new Mock<FeedIterator<Role>>();
                mockFeedIterator.SetupGet(x => x.HasMoreResults).Returns(true);
                mockFeedIterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockFeedResponse.Object);
                
                var mockedItemResponse = new Mock<ItemResponse<User>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.OK);

                container.Setup(q => q.ReplaceItemAsync<User>(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
                container.Setup(x=>x.GetItemQueryIterator<Role>(It.IsAny<QueryDefinition>(),It.IsAny<string>(),It.IsAny<QueryRequestOptions>()))
                    .Returns(mockFeedIterator.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        await target.UpdateRoleAsync(users.First().Roles.Single().name,users.First().Roles.Single(q=>q.name==roleNameToUpdate));
    }
    
    [Test]
    public async Task UpdateRoleAsync_WithErrorResponse_ShouldThrowException()
    {
        var users = base.CreateTestUsers(1).ToList();
        var roleNameToUpdate = users.First().Roles.First().name;
        
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            MutateItems = (items) => items.Where(q=>q.Id == roleNameToUpdate),
            ConfigureContainerFunc = (container) =>
            {
                var mockFeedResponseOfRole = new Mock<FeedResponse<Role>>();
                mockFeedResponseOfRole.SetupGet(x => x.Count).Returns(users.Count);
                mockFeedResponseOfRole.Setup(x => x.GetEnumerator()).Returns(users.First().Roles.GetEnumerator());
                    
                var mockFeedIteratorOfRole = new Mock<FeedIterator<Role>>();
                mockFeedIteratorOfRole.SetupGet(x => x.HasMoreResults).Returns(false);
                mockFeedIteratorOfRole.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(mockFeedResponseOfRole.Object);
                
                var mockedItemResponse = new Mock<ItemResponse<User>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.BadRequest);

                container.Setup(q => q.ReplaceItemAsync<User>(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<RepositoryException>(async () =>
            await target.UpdateRoleAsync(users.First().Roles.Single().name,users.First().Roles.Single(q=>q.name==roleNameToUpdate)));
    }
    
    [Test]
    public async Task UpdateRoleAsync_WithNonExistentRole_ShouldThrowException()
    {
        var nonExistentRoleName = "non-existent";
        
        var users = base.CreateTestUsers(1).ToList();
        
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>()
        {
            // MutateItems = (items) => items.Where(q=>q.roles.Single(q=>q.name) == nonExistentRoleName),
            // ConfigureContainerFunc = (container) =>
            // {
            //     var mockedItemResponse = new Mock<ItemResponse<SecureUser>>();
            //     mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.NotFound);
            //
            //     container.Setup(q => q.ReplaceItemAsync<SecureUser>(It.IsAny<SecureUser>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
            //             It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            //         .ReturnsAsync(mockedItemResponse.Object);
            // }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<RepositoryException>(async () =>
            await target.UpdateRoleAsync(nonExistentRoleName, users.First().Roles.First()));
    }
    
    
    [Test]
    public async Task CreateUserAsync_WithValidUser_ShouldSucceed()
    {
        var users = base.CreateTestUsers(1).ToList();
        var newUser = users.First();
        
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>()
        {
            MutateItems = (items) =>
            {
                items.ToList().Add(newUser);
                return items;
            },
            ConfigureContainerFunc = (container) =>
            {
                var mockedItemResponse = new Mock<ItemResponse<User>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.Created);

                container.Setup(q => q.CreateItemAsync<User>(It.IsAny<User>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        await target.CreateUserAsync(newUser);       
    }
    
    [Test]
    public async Task CreateUserAsync_WithErrorResponse_ShouldThrowException()
    {
        var users = base.CreateTestUsers(1).ToList();
        var newUser = users.First();
        
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>()
        {
            MutateItems = (items) =>
            {
                items.ToList().Add(newUser);
                return items;
            },
            ConfigureContainerFunc = (container) =>
            {
                var mockedItemResponse = new Mock<ItemResponse<User>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.BadRequest);

                container.Setup(q => q.CreateItemAsync<User>(It.IsAny<User>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<RepositoryException>(async () => await target.CreateUserAsync(newUser));   
    }

    [Test]
    public async Task GetApplicationByName_WithEmptyId_ShouldThrowException()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q=>q.Id == users.First().Id)      
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<ArgumentException>(async () => await target.GetApplicationByNameAsync(string.Empty));
    }
    
    [Test]
    public async Task GetApplicationByName_WithExistingId_ShouldReturnApplication()
    {
        var users = base.CreateTestUsers(1).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            ConfigureContainerFunc = (container =>
            {
                var mockFeedResponse = new Mock<FeedResponse<Application>>();
                mockFeedResponse.SetupGet(x => x.Count).Returns(users.First().Roles.First().applications.Count);
                mockFeedResponse.Setup(x => x.GetEnumerator()).Returns(users.First().Roles.First().applications.GetEnumerator());
                
                var mockFeedIterator = new Mock<FeedIterator<Application>>();
                mockFeedIterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockFeedResponse.Object);
                
                container.Setup(x => x.GetItemQueryIterator<Application>(It.IsAny<QueryDefinition>(), It.IsAny<string>(),
                        It.IsAny<QueryRequestOptions>()))
                    .Returns(mockFeedIterator.Object);
            }),
            FilterItems = (items) => items.Where(q=>q.Roles.First().name == users.First().Roles.First().name)            
        };
       
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetApplicationByNameAsync(users.First().Roles.First().applications.First().name);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.name, Is.EqualTo(users.First().Roles.First().applications.First().name));
    }
    
    [Test]
    public async Task GetApplicationByName_WithNonExistentId_ShouldReturnNull()
    {
        var users = base.CreateTestUsers(1).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            ConfigureContainerFunc = (container =>
            {
                var mockFeedResponse = new Mock<FeedResponse<Application>>();
                mockFeedResponse.SetupGet(x => x.Count).Returns(users.First().Roles.First().applications.Count);
                mockFeedResponse.Setup(x => x.GetEnumerator()).Returns(new List<Application>().GetEnumerator());
                
                var mockFeedIterator = new Mock<FeedIterator<Application>>();
                mockFeedIterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockFeedResponse.Object);
                
                container.Setup(x => x.GetItemQueryIterator<Application>(It.IsAny<QueryDefinition>(), It.IsAny<string>(),
                        It.IsAny<QueryRequestOptions>()))
                    .Returns(mockFeedIterator.Object);
            }),
            FilterItems = (items) => items.Where(q=>q.Roles.First().name == users.First().Roles.First().name)            
        };
       
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetApplicationByNameAsync("non-existent");

        Assert.That(result, Is.Null);

    }
}