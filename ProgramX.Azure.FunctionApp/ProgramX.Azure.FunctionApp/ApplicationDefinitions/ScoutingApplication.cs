using ProgramX.Azure.FunctionApp.ApplicationDefinitions.Scouting;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions;

public class ScoutingApplication : IApplication
{
    /// <inheritdoc/>
    public ApplicationMetaData GetApplicationMetaData()
    {
        return new ApplicationMetaData()
        {
            Name = "scouting",
            FriendlyName = "Scouting",
            RequiresRoleNames = ["admin", "pii-reader", "osm-reader", "scouts-reader", "scouts-writer"],
            TargetUrl = "/scouting",
            Description = "Manage scouting data",
            ImageUrl = null
        };
    }

    /// <inheritdoc/>
    public async Task<IApplicationHealthCheck> GetHealthCheckAsync(IUserRepository userRepository)
    {
        return new HealthCheck(this.GetApplicationMetaData(),userRepository);
    }
}

