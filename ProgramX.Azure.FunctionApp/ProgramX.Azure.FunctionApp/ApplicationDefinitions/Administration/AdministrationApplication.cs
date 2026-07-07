using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.HealthChecks;
using ProgramX.Azure.FunctionApp.HealthChecks.Applications;
using ProgramX.Azure.FunctionApp.Model;
using Microsoft.Extensions.Logging;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions.Administration;

public class AdministrationApplication : IApplication
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILoggerFactory _loggerFactory;

    public AdministrationApplication(IUserRepository userRepository,
        IRoleRepository roleRepository,
        ILoggerFactory loggerFactory)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _loggerFactory = loggerFactory;
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
            new AllRequiredRolesAcrossAllUsers(this.GetApplicationMetaData(), _userRepository),
            new AllRequiredRolesDefined(_loggerFactory, this.GetApplicationMetaData(), _roleRepository)
        };
    }

    
}
