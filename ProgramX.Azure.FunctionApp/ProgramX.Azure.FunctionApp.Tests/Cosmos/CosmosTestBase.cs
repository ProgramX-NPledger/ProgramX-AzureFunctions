using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Tests.Cosmos;

public abstract class CosmosTestBase
{
    
    protected IEnumerable<User> CreateTestUsers(int numberOfItems)
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
                
            });
        }

        return users;
    }
}