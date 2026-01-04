using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Tests.Cosmos;

public abstract class CosmosTestBase
{

    
    protected IEnumerable<User> CreateTestUsers(int numberOfItems, int? numberOfRolesPerUser = null, int? numberOfApplicationsPerRole = null)
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
                                metaDataDotNetAssembly = string.Empty,
                                metaDataDotNetType = string.Empty
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