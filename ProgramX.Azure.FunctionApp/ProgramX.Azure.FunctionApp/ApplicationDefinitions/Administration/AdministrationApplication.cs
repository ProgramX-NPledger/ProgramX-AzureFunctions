using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.HealthChecks;
using ProgramX.Azure.FunctionApp.HealthChecks.Applications;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions.Administration;

public class AdministrationApplication : IApplication
{
    private readonly IUserRepository _userRepository;

    public AdministrationApplication(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    /// <inheritdoc/>
    public ApplicationMetaData GetApplicationMetaData()
    {
        return new ApplicationMetaData()
        {
            Name = "administration",
            FriendlyName = "Administration",
            RequiresRoleNames = ["admin"],
            TargetUrl = "/admin",
            Description = "Manage security and global preferences",
            ImageUrl = null
        };
    }

    public IEnumerable<IApplicationHealthCheck> GetHealthChecks()
    {
        return new List<IApplicationHealthCheck>()
        {
            new AllRequiredRolesAcrossAllUsers(this.GetApplicationMetaData(), _userRepository)
        };
    }

    
}