using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.HealthChecks;
using ProgramX.Azure.FunctionApp.HealthChecks.Applications;
using ProgramX.Azure.FunctionApp.Model;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Scouting.Contract;
using ProgramX.Azure.FunctionApp.Scouting.HealthChecks;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions;

public class ScoutingApplication : IApplication
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IScoutingRepository _scoutingRepository;
    private readonly ILoggerFactory _loggerFactory;

    public ScoutingApplication(IUserRepository userRepository,
        IRoleRepository roleRepository,
        IScoutingRepository scoutingRepository,
        ILoggerFactory loggerFactory)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _scoutingRepository = scoutingRepository;
        _loggerFactory = loggerFactory;
    }
    
    /// <inheritdoc/>
    public ApplicationMetaData GetApplicationMetaData()
    {
        return new ApplicationMetaData()
        {
            Name = "scouting",
            FriendlyName = "Scouting",
            RequiresRoleNames = ["scouting"],
            TargetUrl = "/scouting",
            Description = "Scouting applications and integrations with Online Scout Manager (OSM)",
            ImageUrl = null
        };
    }

    public IEnumerable<IApplicationHealthCheck> GetHealthChecks()
    {
        return new List<IApplicationHealthCheck>()
        {
            new AllRequiredRolesAcrossAllUsers(this.GetApplicationMetaData(), _userRepository), 
            new AllRequiredRolesDefined(_loggerFactory, this.GetApplicationMetaData(), _roleRepository),
            new AllScoresDefined(_scoutingRepository),
            // TODO: Database
        };
    }

    
}
