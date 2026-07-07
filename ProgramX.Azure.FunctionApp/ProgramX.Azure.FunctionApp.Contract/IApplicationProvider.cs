using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.Contract;

public interface IApplicationProvider
{
    /// <summary>
    /// Returns all applications.
    /// </summary>
    /// <param name="criteria">Criteria to filter the applications.</param>
    /// <returns>A collection of <see cref="IApplication"/> items.</returns>
    IEnumerable<IApplication> GetAllApplications(GetAllApplicationsCriteria criteria);
    
    /// <summary>
    /// Returns the application with the specified name.
    /// </summary>
    /// <param name="applicationName">Name of the Application to return.</param>
    /// <returns>Requested <see cref="IApplication"/> implementation or <c>null</c> if not found.</returns>
    IApplication? GetApplication(string applicationName);
}