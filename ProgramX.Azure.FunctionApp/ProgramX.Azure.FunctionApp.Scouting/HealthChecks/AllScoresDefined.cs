using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

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
        IEnumerable<ScoutingScore> allRequiredItems = GetRequiredScores();
        
        var allScoutingScores =
            (await _scoutingRepository.GetScoutingActivitiesAsync(new GetScoutingActivitiesCriteria())).Items;
        bool allDefined = allRequiredItems
            .Select(q => q.id)
            .All(q =>
                allScoutingScores
                    .Select(q => q.id)
                    .Contains(q));
        
        return new HealthCheckResult
        {
            IsHealthy = allDefined,
            Message = allDefined
                ? "All required scores are defined"
                : "Some required scores are missing",
            FriendlyName = "All required scores are defined",
            HealthCheckName = nameof(AllScoresDefined)
        };
        
    }

    public async Task<FixApplicationHealthCheckResult> FixHealthAsync(HealthCheckResult healthCheckResult)
    {
        var fixApplicationHealthCheckResultItemResult = new FixApplicationHealthCheckResult()
        {
            Name = "ScoutingScoresDefined",
            IsSuccess = true,
            Messages = new List<string>()
        };
        
        var allRequiredScores = GetRequiredScores();
        foreach (var requiredScore in allRequiredScores)
        {
            // find score
            var scoutingScore = (await _scoutingRepository.GetScoutingActivitiesAsync(new GetScoutingActivitiesCriteria())).Items.SingleOrDefault(q => q.id == requiredScore.id);
            if (scoutingScore == null)
            {
                // if not exist, create it
                await _scoutingRepository.CreateScoreAsync(requiredScore);
                ((List<string>)fixApplicationHealthCheckResultItemResult.Messages).Add($"Created score for {requiredScore.name}");
            }
        }

        return fixApplicationHealthCheckResultItemResult;
        
    }
    
    
    private IEnumerable<ScoutingScore> GetRequiredScores()
    {
        return  new List<ScoutingScore>
        {
            new ScoutingScore()
            {
                id = "attendancePlus",
                name = "Attendance",
                isDynamicallyCalculated = true,
                score = 1,
                ordinal = 1
            },
            new ScoutingScore()
            {
                id = "inspectionMinus",
                name = "Inspection (per uniform infraction)",
                isDynamicallyCalculated = false,
                score = -1,
                ordinal = 10
            },
            new ScoutingScore()
            {
                id = "valueBeliefPlus",
                name = "Value: Belief: +",
                isDynamicallyCalculated = false,
                score = 1,
                ordinal = 20
            },
            new ScoutingScore()
            {
                id = "valueBeliefMinus",
                name = "Value: Belief: -",
                isDynamicallyCalculated = false,
                score = -1,
                ordinal = 21
            },
            new ScoutingScore()
            {
                id = "valueCarePlus",
                name = "Value: Care: +",
                isDynamicallyCalculated = false,
                score = 1,
                ordinal = 30
            },
            new ScoutingScore()
            {
                id = "valueCareMinus",
                name = "Value: Care: -",
                isDynamicallyCalculated = false,
                score = -1,
                ordinal = 31
            },
            new ScoutingScore()
            {
                id = "valueRespectPlus",
                name = "Value: Respect: +",
                isDynamicallyCalculated = false,
                score = 1,
                ordinal = 40
            },
            new ScoutingScore()
            {
                id = "valueRespectMinus",
                name = "Value: Respect: -",
                isDynamicallyCalculated = false,
                score = -1,
                ordinal = 41
            },
            new ScoutingScore()
            {
                id = "valueCooperationPlus",
                name = "Value: Co-operation: +",
                isDynamicallyCalculated = false,
                score = 1,
                ordinal = 50
            },
            new ScoutingScore()
            {
                id = "valueCooperationMinus",
                name = "Value: Co-operation: -",
                isDynamicallyCalculated = false,
                score = -1,
                ordinal = 51
            },
            new ScoutingScore()
            {
                id = "valueIntegrityPlus",
                name = "Value: Integrity: +",
                isDynamicallyCalculated = false,
                score = 1,
                ordinal = 60
            },
            new ScoutingScore()
            {
                id = "valueIntegrityMinus",
                name = "Value: Integrity: -",
                isDynamicallyCalculated = false,
                score = -1,
                ordinal = 61
            },
        };
    }
    
}