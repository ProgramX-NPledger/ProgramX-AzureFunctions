using Microsoft.Azure.Cosmos;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Tests.Mocks;

public class UserRepositoryFactory
{
    
    public static Mock<IUserRepository> CreateUserRepository()
    {
        var testUsers = CreateTestUsers(10);
        
        var mockedRolesResult = new Mock<IResult<Role>>();
        mockedRolesResult.SetupGet(x => x.IsRequiredToBeOrderedByClient)
            .Returns(true);
        mockedRolesResult.SetupGet(x => x.Items)
            .Returns(testUsers.SelectMany(user => user.roles).ToList());
        
        var mockedUserRepository = new Mock<IUserRepository>();
        mockedUserRepository.Setup(x => x.GetRolesAsync(It.IsAny<GetRolesCriteria>(),It.IsAny<PagedCriteria>()))
            .ReturnsAsync(mockedRolesResult.Object);
        mockedUserRepository.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .Callback<User>(user =>
            {
                testUsers.Add(user);
            });
        
        return mockedUserRepository;
    }
    
    
    protected static IList<User> CreateTestUsers(int numberOfItems, int? numberOfRolesPerUser = null, int? numberOfApplicationsPerRole = null)
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
                roles = Enumerable.Range(1,numberOfRolesPerUser ?? numberOfItems)
                    .Select(x => new Role()
                    {
                        createdAt    = DateTime.UtcNow,
                        applications = Enumerable.Range(1, numberOfApplicationsPerRole ?? numberOfItems)
                            .Select(y => new Application
                            {
                                createdAt = DateTime.UtcNow,
                                name = $"app {y}",
                                updatedAt = DateTime.UtcNow,
                                ordinal = y,
                                targetUrl = string.Empty,
                            }),
                        name = $"role {x}",
                        updatedAt = DateTime.UtcNow,
                        description = ">@role{x}",
                    })
            });
        }

        return users;
    }
}