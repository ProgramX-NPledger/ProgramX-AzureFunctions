using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.Contract;

/// <summary>
/// Provides data functionality for Scouting models.
/// </summary>
public interface IScoutingRepository
{
    /// <summary>
    /// Creates the specified Activity.
    /// </summary>
    /// <param name="scoutingActivity">Activity to create.</param>
    Task CreateScoutingActivityAsync(ScoutingActivity scoutingActivity);

    /// <summary>
    /// Gets Scouting Activities.
    /// </summary>
    /// <param name="criteria"></param>
    /// <param name="pagedCriteria"></param>
    /// <returns></returns>
    Task<IResult<ScoutingActivity>> GetScoutingActivitiesAsync(GetScoutingActivitiesCriteria criteria,
        PagedCriteria? pagedCriteria = null);

}