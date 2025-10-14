using System.Net;
using System.Text;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class ApplicationsHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<LoginHttpTrigger> _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

    public ApplicationsHttpTrigger(ILogger<LoginHttpTrigger> logger, 
        CosmosClient cosmosClient, 
        IConfiguration configuration) : base(configuration)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
        
        _container = _cosmosClient.GetContainer("core", "users");
    }

    [Function(nameof(GetApplication))]
    public async Task<HttpResponseData> GetApplication(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "application/{name?}")] HttpRequestData httpRequestData,
        string name)
    {
        return await RequiresAuthentication(httpRequestData, null, async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]);
                var containsText = httpRequestData.Query["containsText"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["containsText"]);
                var withinRoles = httpRequestData.Query["withinRoles"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["withinRoles"]).Split(new [] {','});

                //var sortByColumn = httpRequestData.Query["sortBy"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["sortBy"]);
                var offset = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["offset"]);
                var itemsPerPage = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["itemsPerPage"]);
                
                var pagedCosmosDbApplicationsResults=await GetPagedMultipleItemsAsync(containsText,withinRoles,offset,itemsPerPage);
                var baseUrl =
                    $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";
                var pageUrls = CalculatePageUrls(pagedCosmosDbApplicationsResults,
                    baseUrl,
                    containsText,
                    withinRoles,
                    continuationToken, 
                    offset ?? 0,
                    itemsPerPage ?? DataConstants.ItemsPerPage);
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<Application>(pagedCosmosDbApplicationsResults,pageUrls));
            }
            else
            {
                var application = await GetSingleItemAsync(name);
                if (application == null)
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Application");
                }
                
                //List<Role> roles = user.roles.SelectMany(q=>q.applications).GroupBy(g=>g.name).Select(q=>q.First()).ToList();
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
                {
                    application
                });
            }
        });
    }
    
    
    
    private async  Task<Application?> GetSingleItemAsync(string name)
    {
        QueryDefinition queryDefinition = BuildQueryDefinition(name,null,null);
        
        var applicationsCosmosDbReader = new PagedCosmosDbReader<Application>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName, DataConstants.UserNamePartitionKeyPath);
        PagedCosmosDbResult<Application> pagedCosmosDbResult;
        pagedCosmosDbResult = await applicationsCosmosDbReader.GetPagedItemsAsync(queryDefinition,"c.id");
        
        return pagedCosmosDbResult.Items.FirstOrDefault();
    }


    private IEnumerable<UrlAccessiblePage> CalculatePageUrls(PagedCosmosDbResult<Application> pagedCosmosDbApplicationsResults, 
        string baseUrl, 
        string? containsText, 
        IEnumerable<string>? withinRoles, 
        string? continuationToken,
        int offset=0, 
        int itemsPerPage=DataConstants.ItemsPerPage)
    {
        var totalPages = (int)Math.Ceiling((double)pagedCosmosDbApplicationsResults.TotalItems / itemsPerPage);
        var currentPageNumber = offset==0 ? 1 : (int)Math.Ceiling((double)offset / itemsPerPage);
        
        List<UrlAccessiblePage> pageUrls = new List<UrlAccessiblePage>();
        for (var pageNumber = 1; pageNumber <= totalPages; pageNumber++)
        {
            pageUrls.Add(new UrlAccessiblePage()
            {
                Url = BuildPageUrl(baseUrl, containsText, withinRoles, continuationToken, (pageNumber * itemsPerPage)-itemsPerPage, itemsPerPage),
                PageNumber = pageNumber,
                IsCurrentPage = pageNumber == currentPageNumber,
            });
        }
        return pageUrls;
    }
    
    
    private async Task<PagedCosmosDbResult<Application>> GetPagedMultipleItemsAsync(string? containsText,
        string[]? withinRoles,
        int? offset = 0,
        int? itemsPerPage = DataConstants.ItemsPerPage)
    {
        QueryDefinition queryDefinition = BuildQueryDefinition(null, containsText, withinRoles);
        
        var applicationsCosmosDbReader = new PagedCosmosDbReader<Application>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName, DataConstants.UserNamePartitionKeyPath);
        PagedCosmosDbResult<Application> pagedCosmosDbResult = await applicationsCosmosDbReader.GetPagedItemsAsync(queryDefinition,null,offset,itemsPerPage);
        
        return pagedCosmosDbResult;
    }
    
    
    private string BuildPageUrl(string baseUrl, 
        string? containsText, 
        IEnumerable<string>? withinRoles, 
        string continuationToken, 
        int? offset, 
        int? itemsPerPage)
    {
        var parametersDictionary = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(containsText))
        {
            parametersDictionary.Add("containsText", Uri.EscapeDataString(containsText));
        }

        if (withinRoles != null && withinRoles.Any())
        {
            parametersDictionary.Add("withinRoles", Uri.EscapeDataString(string.Join(",", withinRoles)));
        }

        if (!string.IsNullOrWhiteSpace(continuationToken))
        {
            parametersDictionary.Add("continuationToken", Uri.EscapeDataString(continuationToken));
        }

        if (offset != null)
        {
            parametersDictionary.Add("offset",offset.Value.ToString());
        }

        if (itemsPerPage != null)
        {
            parametersDictionary.Add("itemsPerPage",itemsPerPage.Value.ToString());       
        }
        
        var sb=new StringBuilder(baseUrl);
        if (parametersDictionary.Any())
        {
            sb.Append("?");
            foreach (var param in parametersDictionary)
            {
                sb.Append($"{param.Key}={param.Value}&");
            }
            sb.Remove(sb.Length-1,1);
        }

        return sb.ToString();
    }


    

    private QueryDefinition BuildQueryDefinition(string? name, 
        string? containsText, 
        IEnumerable<string>? withinRoles)
    {
        var sb = new StringBuilder("SELECT a.name, a.description, a.imageUrl, a.targetUrl, a.type, a.schemaVersionNumber, a.isDefaultApplicationOnLogin, a.ordinal, a.createdAt,a.updatedAt FROM c JOIN r IN c.roles JOIN a IN r.applications WHERE 1=1");
        var parameters = new List<(string name, object value)>();
        if (string.IsNullOrWhiteSpace(name))
        {
            if (!string.IsNullOrWhiteSpace(containsText))
            {
                sb.Append(@" AND (
                                CONTAINS(UPPER(a.name), @containsText) OR 
                                CONTAINS(UPPER(a.description), @containsText)
                                )");
                parameters.Add(("@containsText", containsText.ToUpperInvariant()));
            }

            if (withinRoles != null && withinRoles.Any())
            {
                var conditions = new List<string>();
                var rolesList = withinRoles.ToList();
            
                for (int i = 0; i < rolesList.Count; i++)
                {
                    conditions.Add($"EXISTS(SELECT VALUE rr FROM rr IN c.roles WHERE rr.name = @role{i})");
                    parameters.Add(($"@role{i}", rolesList[i]));
                }
            
                sb.Append($" AND ({string.Join(" OR ", conditions)})");
            }
            
            sb.Append(" GROUP BY a.name, a.description, a.imageUrl, a.targetUrl, a.type, a.schemaVersionNumber, a.isDefaultApplicationOnLogin, a.ordinal, a.createdAt,a.updatedAt");
        }
        else
        {
            sb.Append(" AND (a.name=@id)");
            parameters.Add(("@id", name));
        }

        var queryDefinition = new QueryDefinition(sb.ToString());
        foreach (var param in parameters)
        {
            queryDefinition.WithParameter(param.name, param.value);
        }
        return queryDefinition;
        
    }
    
    

    
}