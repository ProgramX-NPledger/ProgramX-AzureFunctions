using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Contract;

public interface IApplication
{
    /// <summary>
    /// Returns the requested application metadata by name.
    /// </summary>
    /// <returns>The requested application metadata or <c>null</c> if not found.</returns>
    ApplicationMetaData GetApplicationMetaData();
    
}