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

    /// <summary>
    /// Creates the specified Score.
    /// </summary>
    /// <param name="scoutingScore">Score to add for allocation to members.</param>
    /// <returns></returns>
    Task CreateScoreAsync(ScoutingScore scoutingScore);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="scoutingScore"></param>
    /// <returns></returns>
    Task AddScoreItemAsync(ScoutingScoreItem scoutingScore);
}