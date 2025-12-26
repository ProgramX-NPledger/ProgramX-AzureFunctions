using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions;

public class ScoutingApplication : IApplication
{
    public ApplicationMetaData GetApplicationMetaData()
    {
        return new ApplicationMetaData()
        {
            name = "scouting",
            FriendlyName = "Scouting",
            requiresRoleNames = ["admin", "pii-reader"],
            targetUrl = "/scouting",
            description = "Manage scouting data",
            imageUrl = null
        };
    }
}

