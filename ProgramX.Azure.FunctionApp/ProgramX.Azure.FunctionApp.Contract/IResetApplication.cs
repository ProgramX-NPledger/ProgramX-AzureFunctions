using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.Contract;

/// <summary>
/// Provides data functionality for <see cref="UserPassword"/>s, <see cref="Role"/>s and <see cref="Application"/>s.
/// </summary>
public interface IResetApplication
{
    Task Reset();
    
    
}