using ProgramX.Azure.FunctionApp.ApplicationDefinitions.Administration;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions;

public class AdministrationApplication : IApplication
{

    /// <inheritdoc/>
    public ApplicationMetaData GetApplicationMetaData()
    {
        return new ApplicationMetaData()
        {
            name = "administration",
            FriendlyName = "Administration",
            requiresRoleNames = ["admin"],
            targetUrl = "/admin",
            description = "Manage security and global preferences",
            imageUrl = null
        };
    }

    /// <inheritdoc/>
    public async Task<IHealthCheck> GetHealthCheckAsync(IUserRepository userRepository)
    {
        return new HealthCheck(this.GetApplicationMetaData(),userRepository);
        
    }
}