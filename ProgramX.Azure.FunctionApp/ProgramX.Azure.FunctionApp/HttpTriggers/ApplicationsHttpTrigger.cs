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


    public ApplicationsHttpTrigger(ILogger<LoginHttpTrigger> logger, CosmosClient cosmosClient, IConfiguration configuration) : base(configuration)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
    }

    [Function(nameof(GetApplications))]
    public async Task<HttpResponseData> GetApplications(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "application/{name?}")] HttpRequestData httpRequestData,
        string name)
    {
        return await RequiresAuthentication(httpRequestData, null, async (_, _) =>
        {
            var pagedAndFilteredCosmosDbReader =
                new PagedCosmosDbReader<Application>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName,DataConstants.UserNamePartitionKeyPath);
            
            var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]);
            var containsText = httpRequestData.Query["containsText"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["containsText"]);
            
            QueryDefinition queryDefinition = BuildQueryDefinition(name,containsText);
            
            var applications = await pagedAndFilteredCosmosDbReader.GetNextItemsAsync(queryDefinition,continuationToken,DataConstants.ItemsPerPage);

            if (string.IsNullOrWhiteSpace(name))
            {
                continuationToken=applications.ContinuationToken;
                var nextPageUrl =
                    BuildNextPageUrl(
                        $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}",
                        containsText, continuationToken);
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData,
                    new PagedResponse<Application>(applications, nextPageUrl,Enumerable.Empty<UrlAccessiblePage>()));
            }
            else
            {
                var application = applications.Items.FirstOrDefault(q=>q.name==name);
                if (application == null)
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Application");
                }
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
                {
                    application
                });
            }
        });
        
        

    }
    
    
    private string BuildNextPageUrl(string baseUrl, string? containsText, string? continuationToken)
    {
        var parametersDictionary = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(containsText))
        {
            parametersDictionary.Add("containsText", Uri.EscapeDataString(containsText));
        }

        if (!string.IsNullOrWhiteSpace(continuationToken))
        {
            parametersDictionary.Add("continuationToken", Uri.EscapeDataString(continuationToken));
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

    private QueryDefinition BuildQueryDefinition(string? id, string? containsText)
    {
        var sb = new StringBuilder("SELECT a.name,a.description,a.imageUrl,a.targetUrl,a.type,a.schemaVersionNumber,a.isDefaultApplicationOnLogin,a.ordinal FROM c JOIN r IN c.roles JOIN a IN r.applications WHERE 1=1");
        var parameters = new List<(string name, object value)>();
        if (string.IsNullOrWhiteSpace(id))
        {
            if (!string.IsNullOrWhiteSpace(containsText))
            {
                sb.Append(@" AND (
                                CONTAINS(UPPER(a.name), @containsText) OR 
                                CONTAINS(UPPER(a.description), @containsText)
                                )");
                parameters.Add(("@containsText", containsText.ToUpperInvariant()));
            }

        
            

        }
        else
        {
            sb.Append(" AND (a.id=@id OR a.name=@id)");
            parameters.Add(("@id", id));
        }

        var queryDefinition = new QueryDefinition(sb.ToString());
        foreach (var param in parameters)
        {
            queryDefinition.WithParameter(param.name, param.value);
        }
        return queryDefinition;
        
    }

    
}