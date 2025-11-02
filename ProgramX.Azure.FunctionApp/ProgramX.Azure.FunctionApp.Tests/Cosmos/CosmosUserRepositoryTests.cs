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
            .SelectMany(q => q.roles)
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
            .SelectMany(q => q.roles)
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
            .SelectMany(q => q.roles)
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

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<SecureUser>(users);
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

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<SecureUser>(users)
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

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<SecureUser>(users)
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
            .SelectMany(q => q.roles)
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
            .SelectMany(q => q.roles)
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
            .SelectMany(q => q.roles)
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
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<SecureUser>(users);
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

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<SecureUser>(users)
        {
            FilterItems = (items) => items.Where(q=>q.id == users.First().id)      
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetUserByIdAsync(users.First().id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.id, Is.EqualTo(users.First().id));
    }
    
    
    [Test]
    public async Task GetUserByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        var nonExistentId = "non-existent";
        
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<SecureUser>(users)
        {
            FilterItems = (items) => items.Where(q=>q.id == nonExistentId)      
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

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<SecureUser>(users)
        {
            FilterItems = (items) => items.Where(q=>q.userName == users.First().userName)      
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetUserByIdAsync(users.First().userName);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.id, Is.EqualTo(users.First().id));      
    }
    
   [Test]
    public async Task GetUserByUserNameAsync_WithNonExistentUserName_ShouldReturnNull()
    {
        var nonExistentUserName = "non-existent";
        
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<SecureUser>(users)
        {
            FilterItems = (items) => items.Where(q=>q.userName == nonExistentUserName)      
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
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<SecureUser>();
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<ArgumentException>(async () => await target.DeleteUserByIdAsync(string.Empty));
    }
    
    [Test]
    public void DeleteUserByIdAsync_WithErrorResponse_ShouldThrowException()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>()
        {
            ConfigureContainerFunc = (container) =>
            {
                var mockedItemResponse = new Mock<ItemResponse<User>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.BadRequest);

                container.Setup(q => q.DeleteItemAsync<User>(It.IsAny<string>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<RepositoryException>(async () => await target.DeleteUserByIdAsync(users.First().id));
    }
    
    [Test]
    public async Task DeleteUserByIdAsync_WithExistingId_ShouldSucceed()
    {
        var users = base.CreateTestUsers(5).ToList();
        var userIdToDelete = users.First().id;
        
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>()
        {
            MutateItems = (items) => items.Where(q=>q.id != userIdToDelete),
            ConfigureContainerFunc = (container) =>
            {
                var mockedItemResponse = new Mock<ItemResponse<User>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.NoContent);

                container.Setup(q => q.DeleteItemAsync<User>(It.IsAny<string>(), It.IsAny<PartitionKey>(),
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
    public async Task GetInsecureUserById_WithEmptyId_ShouldThrowException()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q=>q.id == users.First().id)      
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<ArgumentException>(async () => await target.GetInsecureUserByIdAsync(string.Empty));
    }
    
    [Test]
    public async Task GetInsecureUserById_WithExistingId_ShouldReturnUser()
    {
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q=>q.id == users.First().id)      
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetInsecureUserByIdAsync(users.First().id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.id, Is.EqualTo(users.First().id));
    }
    
    [Test]
    public async Task GetInsecureUserById_WithNonExistentId_ShouldReturnNull()
    {
        var nonExistentId = "non-existent";
        
        var users = base.CreateTestUsers(5).ToList();

        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<User>(users)
        {
            FilterItems = (items) => items.Where(q=>q.id == nonExistentId)      
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        var result = await target.GetInsecureUserByIdAsync(nonExistentId);

        Assert.That(result, Is.Null);
    }
    
    [Test]
    public async Task UpdateUserAsync_WithValidSecureUser_ShouldSucceed()
    {
        var users = base.CreateTestUsers(5).ToList();
        var userIdToUpdate = users.First().id;
        
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<SecureUser>()
        {
            MutateItems = (items) => items.Where(q=>q.id == userIdToUpdate),
            ConfigureContainerFunc = (container) =>
            {
                var mockedItemResponse = new Mock<ItemResponse<SecureUser>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.OK);

                container.Setup(q => q.ReplaceItemAsync<SecureUser>(It.IsAny<SecureUser>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        await target.UpdateUserAsync(users.Single(q=>q.id==userIdToUpdate));
    }
    
    [Test]
    public async Task UpdateUserAsync_WithErrorResponse_ShouldThrowException()
    {
        var users = base.CreateTestUsers(5).ToList();
        var userIdToUpdate = users.First().id;
        
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<SecureUser>()
        {
            MutateItems = (items) => items.Where(q=>q.id == userIdToUpdate),
            ConfigureContainerFunc = (container) =>
            {
                var mockedItemResponse = new Mock<ItemResponse<SecureUser>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.BadRequest);

                container.Setup(q => q.ReplaceItemAsync<SecureUser>(It.IsAny<SecureUser>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<RepositoryException>(async () =>
            await target.UpdateUserAsync(users.Single(q => q.id == userIdToUpdate)));
    }
    
    [Test]
    public async Task UpdateUserAsync_WithNonExistentUser_ShouldThrowException()
    {
        var users = base.CreateTestUsers(5).ToList();
        var userIdToUpdate = users.First().id;
        
        var mockCosmosClientFactory = new MockedCosmosDbClientFactory<SecureUser>()
        {
            MutateItems = (items) => items.Where(q=>q.id == userIdToUpdate),
            ConfigureContainerFunc = (container) =>
            {
                var mockedItemResponse = new Mock<ItemResponse<SecureUser>>();
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.NotFound);

                container.Setup(q => q.ReplaceItemAsync<SecureUser>(It.IsAny<SecureUser>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockedItemResponse.Object);
            }
        };
        var mockCosmosClient = mockCosmosClientFactory.Create();

        var mockLogger = new Mock<ILogger<CosmosUserRepository>>();

        var target = new CosmosUserRepository(mockCosmosClient.MockedCosmosClient.Object, mockLogger.Object);
        Assert.ThrowsAsync<RepositoryException>(async () =>
            await target.UpdateUserAsync(users.Single(q => q.id == userIdToUpdate)));
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
                mockedItemResponse.SetupGet(x => x.StatusCode).Returns(System.Net.HttpStatusCode.OK);

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

}