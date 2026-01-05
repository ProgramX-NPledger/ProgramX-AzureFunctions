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
            name = "scouting",
            FriendlyName = "Scouting",
            requiresRoleNames = ["admin", "pii-reader", "osm-reader", "scouts-reader", "scouts-writer"],
            targetUrl = "/scouting",
            description = "Manage scouting data",
            imageUrl = null
        };
    }

    public async Task<IHealthCheck> GetHealthCheckAsync(IUserRepository userRepository)
    {
        return new HealthCheck(this.GetApplicationMetaData(),userRepository);
    }
}

