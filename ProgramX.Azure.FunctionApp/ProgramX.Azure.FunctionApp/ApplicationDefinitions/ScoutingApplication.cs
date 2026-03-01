using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Scouting.Administration;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions;

public class ScoutingApplication : IApplication
{
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

    public async Task<IHealthCheck> GetHealthCheckAsync(IUserRepository userRepository)
    {
        return new HealthCheck(this.GetApplicationMetaData(),userRepository);
    }
}

