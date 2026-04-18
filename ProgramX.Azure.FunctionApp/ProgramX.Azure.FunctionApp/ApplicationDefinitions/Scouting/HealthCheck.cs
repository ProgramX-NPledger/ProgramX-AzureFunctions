using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions.Scouting;

public class HealthCheck : ApplicationHealthCheckBase, IApplicationHealthCheck
{
    private readonly IScoutingRepository _scoutingRepository;
    private readonly ILogger<HealthCheck> _logger;    
    
    public HealthCheck(ApplicationMetaData applicationMetaData, 
        IUserRepository userRepository,
        IScoutingRepository scoutingRepository)
        : base(applicationMetaData, userRepository)
    {
        _scoutingRepository = scoutingRepository;
        _logger = new LoggerFactory().CreateLogger<HealthCheck>();
    }
    
    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        var result = new HealthCheckResult()
        {
            HealthCheckName = MetaData.FriendlyName,
            Items = new List<HealthCheckItemResult>()
            {
                await GetHealthCheckForAllRolesAcrossAllUsersAsync(),
                await GetHealthCheckForAllScoresDefinedAsync()
            }
        };
        result.IsHealthy = result.Items.All(q=>q.IsHealthy ?? false);
        result.Message = result.IsHealthy ? "OK" : "Problem(s) found";

        return result;

    }

    /// <summary>
    /// Ensures all scores are defined.
    /// </summary>
    /// <returns></returns>
    private async Task<HealthCheckItemResult> GetHealthCheckForAllScoresDefinedAsync()
    {
        IEnumerable<ScoutingScore> allRequiredItems = GetRequiredScores();
        var allScoutingScores = (await _scoutingRepository.GetScoutingActivitiesAsync(new GetScoutingActivitiesCriteria())).Items;
        bool allDefined = allRequiredItems
            .Select(q => q.id)
            .All(q => 
                allScoutingScores
                    .Select(q => q.id)
                    .Contains(q));
        return new HealthCheckItemResult
        {
            IsHealthy = allDefined,
            Message = allDefined
                ? "All required scores are defined"
                : "Some required scores are missing",
            FriendlyName = "All required scores are defined",
            Name = "ScoutingScoresDefined"
        };
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
    
    /// <inheritdoc/>
    public async Task<FixApplicationHealthCheckResult> FixHealthAsync(HealthCheckResult healthCheckResult)
    {
        List<FixApplicationHealthCheckResultItemResult> fixedItems = new List<FixApplicationHealthCheckResultItemResult>();
        
        // check for required roles
        var allRolesAcrossAllUsersHealthCheck = healthCheckResult.Items.SingleOrDefault(q => q.Name == "AllRolesAcrossAllUsers");
        if (allRolesAcrossAllUsersHealthCheck != null && allRolesAcrossAllUsersHealthCheck.IsHealthy == false)
        {
            var result = await FixHealthCheckForAllRolesAcrossAllUsersAsync(await CreateApplicationAsync());
           fixedItems.Add(result);
        }

        // scores
        var allScoresDefinedHealthCheck = healthCheckResult.Items.SingleOrDefault(q => q.Name == "ScoutingScoresDefined");
        if (allScoresDefinedHealthCheck != null && allScoresDefinedHealthCheck.IsHealthy == false)
        {
            var result = await FixHealthCheckForAllScoresDefinedAsync();
            fixedItems.Add(result);
        }
        
        return new FixApplicationHealthCheckResult()
        {
            Items = fixedItems
        };
    }

    private async Task<FixApplicationHealthCheckResultItemResult> FixHealthCheckForAllScoresDefinedAsync()
    {
        var fixApplicationHealthCheckResultItemResult = new FixApplicationHealthCheckResultItemResult()
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


    private async Task<Application> CreateApplicationAsync()
    {
        var allApplications = await UserRepository.GetApplicationsAsync(new GetApplicationsCriteria());
        var applicationWithHighestOrdinal = allApplications.Items.OrderByDescending(q => q.ordinal).FirstOrDefault();

        return new Application()
        {
            name = MetaData.Name,
            friendlyName = MetaData.FriendlyName,
            createdAt = DateTime.UtcNow,
            isDefaultApplicationOnLogin = false,
            ordinal = (applicationWithHighestOrdinal?.ordinal ?? 0) + 1,
            schemaVersionNumber = 3,
            updatedAt = DateTime.UtcNow,
            metaDataDotNetAssembly = MetaData.GetType().Assembly.GetName().Name,
            metaDataDotNetType = MetaData.GetType().Name
        };
    }

}