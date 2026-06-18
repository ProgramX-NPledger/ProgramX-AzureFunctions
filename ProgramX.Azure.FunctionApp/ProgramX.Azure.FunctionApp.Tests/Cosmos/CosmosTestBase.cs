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
                Id = Guid.NewGuid().ToString(),
                UserName = $"user{i}",
                emailAddress = $"",
                CreatedAt = DateTime.UtcNow,
                FirstName = $"First Name {i}",
                LastLoginAt = DateTime.UtcNow,
                LastName = $"Last Name {i}",
                UpdatedAt = DateTime.UtcNow,
                Roles = Enumerable.Range(1,numberOfRolesPerUser ?? numberOfItems)
                    .Select(x => new Role()
                    {
                        createdAt    = DateTime.UtcNow,
                        applications = Enumerable.Range(1, numberOfApplicationsPerRole ?? numberOfItems)
                            .Select(y => new Application
                            {
                                createdAt = DateTime.UtcNow,
                                name = $"app {y}",
                                updatedAt = DateTime.UtcNow,
                                ordinal = y
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