using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Scouting.Contract;
using ProgramX.Azure.FunctionApp.Scouting.Model;

namespace ProgramX.Azure.FunctionApp.Scouting.HealthChecks;

public class AllScoresDefined : IApplicationHealthCheck
{
    private readonly IScoutingRepository _scoutingRepository;

    public AllScoresDefined(IScoutingRepository scoutingRepository)
    {
        _scoutingRepository = scoutingRepository;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync()
    {

        var missingScores = await GetMissingScoresAsync();
        
        return new HealthCheckResult
        {
            IsHealthy = !missingScores.Any(),
            Message = !missingScores.Any()
                ? "All required scores are defined"
                : $"The following required Scores are missing: {string.Join(", ", missingScores.Select(s => s.Name))}",
            FriendlyName = "All required scores are defined",
            HealthCheckName = nameof(AllScoresDefined)
        };
        
    }

    /// <summary>
    /// Returns a list of required scores that are missing from the database.
    /// </summary>
    /// <returns></returns>
    public async Task<List<ScoutingScore>> GetMissingScoresAsync()
    {
        IEnumerable<ScoutingScore> allRequiredItems = GetRequiredScores();

        var allScoutingScores =
            (await _scoutingRepository.GetScoutingScoresAsync(new GetScoutingScoresCriteria())).Items;
        
        return allRequiredItems
            .Where(requiredItem => !allScoutingScores
                .Select(s => s.Id)
                .Contains(requiredItem.Id))
            .ToList<ScoutingScore>();

    }


    private IEnumerable<ScoutingScore> GetRequiredScores()
    {
        return  new List<ScoutingScore>
        {
            new ScoutingScore()
            {
                Id = "attendancePlus",
                Name = "Attendance",
                IsDynamicallyCalculated = true,
                Score = 1,
                Ordinal = 1
            },
            new ScoutingScore()
            {
                Id = "attendanceMinus",
                Name = "Attendance",
                IsDynamicallyCalculated = true,
                Score = -1,
                Ordinal = 2
            },
            new ScoutingScore()
            {
                Id = "inspectionMinus",
                Name = "Inspection (per uniform infraction)",
                IsDynamicallyCalculated = false,
                Score = -1,
                Ordinal = 10
            },
            new ScoutingScore()
            {
                Id = "valueBeliefPlus",
                Name = "Value: Belief: +",
                IsDynamicallyCalculated = false,
                Score = 1,
                Ordinal = 20
            },
            new ScoutingScore()
            {
                Id = "valueBeliefMinus",
                Name = "Value: Belief: -",
                IsDynamicallyCalculated = false,
                Score = -1,
                Ordinal = 21
            },
            new ScoutingScore()
            {
                Id = "valueCarePlus",
                Name = "Value: Care: +",
                IsDynamicallyCalculated = false,
                Score = 1,
                Ordinal = 30
            },
            new ScoutingScore()
            {
                Id = "valueCareMinus",
                Name = "Value: Care: -",
                IsDynamicallyCalculated = false,
                Score = -1,
                Ordinal = 31
            },
            new ScoutingScore()
            {
                Id = "valueRespectPlus",
                Name = "Value: Respect: +",
                IsDynamicallyCalculated = false,
                Score = 1,
                Ordinal = 40
            },
            new ScoutingScore()
            {
                Id = "valueRespectMinus",
                Name = "Value: Respect: -",
                IsDynamicallyCalculated = false,
                Score = -1,
                Ordinal = 41
            },
            new ScoutingScore()
            {
                Id = "valueCooperationPlus",
                Name = "Value: Co-operation: +",
                IsDynamicallyCalculated = false,
                Score = 1,
                Ordinal = 50
            },
            new ScoutingScore()
            {
                Id = "valueCooperationMinus",
                Name = "Value: Co-operation: -",
                IsDynamicallyCalculated = false,
                Score = -1,
                Ordinal = 51
            },
            new ScoutingScore()
            {
                Id = "valueIntegrityPlus",
                Name = "Value: Integrity: +",
                IsDynamicallyCalculated = false,
                Score = 1,
                Ordinal = 60
            },
            new ScoutingScore()
            {
                Id = "valueIntegrityMinus",
                Name = "Value: Integrity: -",
                IsDynamicallyCalculated = false,
                Score = -1,
                Ordinal = 61
            },
        };
    }
    
}