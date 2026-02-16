using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.Cosmos;

public class CosmosScoutingRepository(CosmosClient cosmosClient, ILogger<CosmosScoutingRepository> logger) : IScoutingRepository
{
    /// <inheritdoc />
    /// <exception cref="RepositoryException">Thrown if the creation failed.</exception>
    public async Task CreateScoutingActivityAsync(ScoutingActivity scoutingActivity)
    {
        using (logger.BeginScope("CreateScoutingActivityAsync {scoutingActivity}", scoutingActivity))
        {
            var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseNames.Scouting);
            var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(ContainerNames.ScoutingActivities, ContainerNames.ScoutingActivityPartitionKey);

            var response = await containerResponse.Container.CreateItemAsync(scoutingActivity, new PartitionKey(scoutingActivity.id));

            if (response.StatusCode != HttpStatusCode.Created)
            {
                logger.LogError(
                    "Failed to create {type} with id {id} with status code {statusCode} and response {response}", nameof(ScoutingActivity),scoutingActivity.id,
                    response.StatusCode, response);
                throw new RepositoryException(OperationType.Create, typeof(ScoutingActivity));
            }
            logger.LogDebug("Success");
        }    
    }
    
    
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown if required initialisation properties are <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Thrown if required parameters are <c>null</c>.</exception>
    public async Task<IResult<ScoutingActivity>> GetScoutingActivitiesAsync(GetScoutingActivitiesCriteria criteria, 
        PagedCriteria? pagedCriteria = null)
    {  
        using (logger.BeginScope("GetScoutingActivitiesAsync {criteria}, {pagedCriteria}", criteria, pagedCriteria?.ToString() ?? "null"))
        {
            QueryDefinition queryDefinition = BuildQueryDefinitionForScoutingActivities(criteria);
            logger.LogDebug("QueryDefinition: {queryDefinition}", queryDefinition);
            
            CosmosReader<ScoutingActivity> cosmosReader;
            IResult<ScoutingActivity> result;
            if (pagedCriteria != null)
            {
                cosmosReader = new CosmosPagedReader<ScoutingActivity>(cosmosClient,
                    DatabaseNames.Core,
                    ContainerNames.Users,
                    ContainerNames.UserNamePartitionKey);
                result = await ((CosmosPagedReader<ScoutingActivity>)cosmosReader).GetPagedItemsAsync(queryDefinition,
                    pagedCriteria.Offset,
                    pagedCriteria.ItemsPerPage);
            }
            else
            {
                cosmosReader = new CosmosReader<ScoutingActivity>(cosmosClient,
                    DatabaseNames.Core,
                    ContainerNames.Users,
                    ContainerNames.UserNamePartitionKey);
                result = await cosmosReader.GetItemsAsync(queryDefinition);
            }

            logger.LogDebug("Result: {result}", result);
            result.IsRequiredToBeOrderedByClient = false;
            return result;
        }
    }

    private QueryDefinition BuildQueryDefinitionForScoutingActivities(GetScoutingActivitiesCriteria criteria)
    {
        var sb = new StringBuilder(@"SELECT c.id, c.activityLocation, c.activityFormat, c.activityType, c.winModes,
        c.title, c.resources, c.preparationMarkdown, c.summary, c.descriptionMarkdown, c.referencesMarkdown, c.tags, 
        c.sections, c.contributesTowardsOsmBadgeName, c.contributesTowardsOsmBadgeId, c.createdAt, c.updatedAt,
        c.schemaVersionNumber FROM c WHERE 1=1");
        var parameters = new List<(string name, object value)>();
        
        if (!string.IsNullOrWhiteSpace(criteria.Id))
        {
            sb.Append(" AND (c.id=@id)");
            parameters.Add(("@id", criteria.Id));
        }

        if (criteria.AnyOfSections != null && criteria.AnyOfSections.Any())
        {
            var conditions = new List<string>();
        
            for (int i = 0; i < criteria.AnyOfSections.Count(); i++)
            {
                conditions.Add($"EXISTS(SELECT VALUE s FROM s IN c.sections WHERE w = @section{i})");
                parameters.Add(($"@section{i}", criteria.AnyOfSections.ElementAt(i)));
            }
        
            sb.Append($" AND ({string.Join(" OR ", conditions)})");
        }
        
        if (criteria.AnyOfWinModes != null && criteria.AnyOfWinModes.Any())
        {
            var conditions = new List<string>();
        
            for (int i = 0; i < criteria.AnyOfWinModes.Count(); i++)
            {
                conditions.Add($"EXISTS(SELECT VALUE w FROM w IN c.winModes WHERE w = @winMode{i})");
                parameters.Add(($"@winMode{i}", criteria.AnyOfWinModes.ElementAt(i)));
            }
        
            sb.Append($" AND ({string.Join(" OR ", conditions)})");
        }
        
        if (criteria.AnyOfActivityLocations != null && criteria.AnyOfActivityLocations.Any())
        {
            var conditions = new List<string>();
        
            for (int i = 0; i < criteria.AnyOfActivityLocations.Count(); i++)
            {
                conditions.Add($"EXISTS(SELECT VALUE l FROM l IN c.activityLocations WHERE l = @activityLocation{i})");
                parameters.Add(($"@activityLocation{i}", criteria.AnyOfActivityLocations.ElementAt(i)));
            }
        
            sb.Append($" AND ({string.Join(" OR ", conditions)})");
        }
        
        if (criteria.AnyOfActivityFormats != null && criteria.AnyOfActivityFormats.Any())
        {
            var conditions = new List<string>();
        
            for (int i = 0; i < criteria.AnyOfActivityFormats.Count(); i++)
            {
                conditions.Add($"EXISTS(SELECT VALUE f FROM f IN c.activityFormat WHERE f = @activityFormat{i})");
                parameters.Add(($"@activityFormat{i}", criteria.AnyOfActivityFormats.ElementAt(i)));
            }
        
            sb.Append($" AND ({string.Join(" OR ", conditions)})");
        }

        if (criteria.AnyOfActivityTypes != null && criteria.AnyOfActivityTypes.Any())
        {
            var conditions = new List<string>();
        
            for (int i = 0; i < criteria.AnyOfActivityTypes.Count(); i++)
            {
                conditions.Add($"EXISTS(SELECT VALUE f FROM f IN c.activityType WHERE f = @activityType{i})");
                parameters.Add(($"@activityType{i}", criteria.AnyOfActivityTypes.ElementAt(i)));
            }
        
            sb.Append($" AND ({string.Join(" OR ", conditions)})");
        }
        
        if (criteria.ContributesTowardsAnyOsmBadgeId != null && criteria.ContributesTowardsAnyOsmBadgeId.Any())
        {
            var conditions = new List<string>();
        
            for (int i = 0; i < criteria.ContributesTowardsAnyOsmBadgeId.Count(); i++)
            {
                conditions.Add($"EXISTS(SELECT VALUE b FROM b IN c.contributesTowardsOsmBadgeId WHERE b = @osmBadgeId{i})");
                parameters.Add(($"@osmBadgeId{i}", criteria.ContributesTowardsAnyOsmBadgeId.ElementAt(i)));
            }
        
            sb.Append($" AND ({string.Join(" OR ", conditions)})");
        }
        
        if (!string.IsNullOrWhiteSpace(criteria.ContainingText))
        {
            var keywords = criteria.ContainingText.Split(' ');

            sb.Append(@" AND (");
            var i = 0;
            foreach (var keyword in keywords)
            {
                sb.Append($"CONTAINS(UPPER(c.title), @containsText{i}) OR");
                sb.Append($"CONTAINS(UPPER(c.preparationMarkdown), @containsText{i}) OR");
                sb.Append($"CONTAINS(UPPER(c.summary), @containsText{i}) OR");
                sb.Append($"CONTAINS(UPPER(c.descriptionMarkdown), @containsText{i}) OR");
                sb.Append($"CONTAINS(UPPER(c.preparationMarkdown), @containsText{i}) OR");
                sb.Append($"CONTAINS(UPPER(c.referencesMarkdown), @containsText{i}) OR");
                sb.Append($"CONTAINS(UPPER(c.title), @containsText{i})");
                sb.Append($"CONTAINS(UPPER(c.contributesTowardsOsmBadgeName), @containsText{i})");
                parameters.Add(($"@containsText{i}", keyword.ToUpperInvariant()));
            }
            sb.Append(")");
        }
        
        var queryDefinition = new QueryDefinition(sb.ToString());
        foreach (var param in parameters)
        {
            queryDefinition.WithParameter(param.name, param.value);
        }
        return queryDefinition;
        
    }
}