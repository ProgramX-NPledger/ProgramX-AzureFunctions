using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Scouting.HealthChecks;

namespace ProgramX.Azure.FunctionApp.Scouting;

public class Application : IApplication
{
    private readonly IScoutingRepository _scoutingRepository;
    private readonly IUserRepository _userRepository;

    public Application(IScoutingRepository scoutingRepository, 
        IUserRepository userRepository)
    {
        _scoutingRepository = scoutingRepository;
        _userRepository = userRepository;
    }
    
    /// <inheritdoc/>
    public ApplicationMetaData GetApplicationMetaData()
    {
        return new ApplicationMetaData()
        {
            Name = "scouting",
            FriendlyName = "Scouting",
            RequiresRoleNames = ["admin", "scouts-reader", "scouts-writer"],
            TargetUrl = "/scouting",
            Description = "Manage scouting data",
            ImageUrl = null
        };
    }

    /// <inheritdoc/>
    public IEnumerable<IApplicationHealthCheck> GetHealthChecks()
    {
        return new List<IApplicationHealthCheck>
        {
            new AllScoresDefined(_scoutingRepository)
        };
    }
}