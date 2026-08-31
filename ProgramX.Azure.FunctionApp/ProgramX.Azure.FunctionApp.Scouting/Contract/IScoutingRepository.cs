using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Scouting.Model;

namespace ProgramX.Azure.FunctionApp.Scouting.Contract;

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
    /// <param name="id">The unique identifier of the Score.</param>
    /// <param name="name">Name of the Score.</param>
    /// <param name="score">Value of the Score.</param>
    /// <param name="isDynamicallyCalculated">Set to <c>True</c> if this Score will be dynamically calculated.</param>
    /// <param name="ordinal">Ordinal of the Score. Specify <c>null</c> to calculate the next ordinal.</param>
    /// <returns>The created <see cref="ScoutingScore"/>.</returns>
    Task<ScoutingScore> CreateScoreAsync(string id, string name, int score, bool isDynamicallyCalculated, int? ordinal);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="scoutingScore"></param>
    /// <returns></returns>
    Task AddScoreItemAsync(ScoutingScoreItem scoutingScore);

    /// <summary>
    /// Gets Scouting Scores.
    /// </summary>
    /// <param name="criteria"></param>
    /// <param name="pagedCriteria"></param>
    /// <returns></returns>
    Task<IResult<ScoutingScore>> GetScoutingScoresAsync(GetScoutingScoresCriteria criteria, PagedCriteria? pagedCriteria = null);


    /// <summary>
    /// Creates a new scouting score item.
    /// </summary>
    /// <param name="osmMemberId">The OSM Member ID.</param>
    /// <param name="date">The date that the score will be recorded for.</param>
    /// <param name="scoreName">The name of the Score.</param>
    /// <param name="score">The value of the Score.</param>
    /// <param name="patrolName">The name of the Patrol of the Member.</param>
    /// <param name="notes">Notes attached to the Score, if any.</param>
    /// <returns>The created <see cref="ScoutingScoreItem"/>.</returns>
    Task<ScoutingScoreItem> CreateScoutingScoreItemAsync(int osmMemberId, DateOnly date, string scoreName, int score,
        string patrolName, string? notes);

    /// <summary>
    /// Gets Scouting Score Items.
    /// </summary>
    /// <param name="criteria"></param>
    /// <param name="pagedCriteria"></param>
    /// <returns></returns>
    Task<IResult<ScoutingScoreItem>> GetScoutingScoreItemsAsync(GetScoutingScoreItemsCriteria criteria,
        PagedCriteria? pagedCriteria = null);

    /// <summary>
    /// Gets a Scouting Score Item by Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ScoutingScoreItem?> GetScoutingScoreItemByIdAsync(string id);
    

}