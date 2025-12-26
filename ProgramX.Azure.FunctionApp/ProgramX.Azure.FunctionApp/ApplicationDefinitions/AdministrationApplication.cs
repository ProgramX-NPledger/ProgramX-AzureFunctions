using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions;

public class AdministrationApplication : IApplication
{
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
}