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
using ProgramX.Azure.FunctionApp.ApplicationDefinitions;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Constants;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class ApplicationsHttpTrigger(
    ILogger<ApplicationsHttpTrigger> logger,
    IConfiguration configuration,
    IUserRepository userRepository)
    : AuthorisedHttpTriggerBase(configuration)
{
    private readonly ILogger<ApplicationsHttpTrigger> _logger = logger;

    
    [Function(nameof(GetApplication))]
    public async Task<HttpResponseData> GetApplication(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "application/{name?}")] HttpRequestData httpRequestData,
        string? name)
    {
        return await RequiresAuthentication(httpRequestData, null, async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]!);
                var containsText = httpRequestData.Query["containsText"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["containsText"]!);
                var withinRoles = httpRequestData.Query["withinRoles"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["withinRoles"]!).Split(
                    [',']);
                var offset = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["offset"]) ?? 0;
                var itemsPerPage = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["itemsPerPage"]) ?? PagingConstants.ItemsPerPage;
                
                var applications = await userRepository.GetApplicationsAsync(new GetApplicationsCriteria()
                {
                    WithinRoles = withinRoles,
                    ContainingText = containsText
                }, new PagedCriteria()
                {
                    ItemsPerPage = itemsPerPage,
                    Offset = offset
                });
                
                var baseUrl =
                    $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";
                
                var pageUrls = CalculatePageUrls((IPagedResult<Application>)applications,
                    baseUrl,
                    containsText,
                    withinRoles,
                    continuationToken, 
                    offset,
                    itemsPerPage);
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<Application>((IPagedResult<Application>)applications,pageUrls));
                
            }
            else
            {
                var application = await userRepository.GetApplicationByNameAsync(name);
                if (application==null)
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Application");
                }

                var withinRoles = userRepository.GetRolesAsync(new GetRolesCriteria()
                {
                    UsedInApplicationNames = [name]
                });
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
                {
                    application,
                    usedInRoles = withinRoles.Result.Items
                });
            }
        });
    }
    
    private IEnumerable<UrlAccessiblePage> CalculatePageUrls(IPagedResult<Application> pagedResults, 
        string baseUrl, 
        string? containsText, 
        IEnumerable<string>? withinRoles, 
        string? continuationToken,
        int offset=0, 
        int itemsPerPage=PagingConstants.ItemsPerPage)
    {
        var currentPageNumber = offset==0 ? 1 : (int)Math.Ceiling((offset + 1.0) / itemsPerPage);
        
        List<UrlAccessiblePage> pageUrls = new List<UrlAccessiblePage>();
        for (var pageNumber = 1; pageNumber <= pagedResults.NumberOfPages; pageNumber++)
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
    
    
    [Function(nameof(GetHealthCheck))]
    public async Task<HttpResponseData> GetHealthCheck(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "application/{name}/health")] HttpRequestData httpRequestData, string name)
    {
        return await RequiresAuthentication(httpRequestData, null, async (_, _) =>
        {
            IHealthCheck? healthCheck = await GetHealthCheckByNameAsync(name);
            if (healthCheck != null)
            {
                var healthCheckResult = await healthCheck.CheckHealthAsync();

                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData,
                    new GetHealthCheckServiceResponse()
                    {
                        Name = name,
                        IsHealthy = healthCheckResult.IsHealthy,
                        Message = healthCheckResult.Message,
                        TimeStamp = DateTime.UtcNow,
                        SubItems = healthCheckResult.Items ?? new List<HealthCheckItemResult>()
                    });

            }
            else
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,
                    $"No health check found for {name}");
            }
        });

    }

    private async Task<IHealthCheck?> GetHealthCheckByNameAsync(string name)
    {
        // get application by name
        var application = await userRepository.GetApplicationByNameAsync(name);
        if (application == null) return null;
        
        // get health check
        var iApplication = ApplicationFactory.GetApplicationForApplicationName(application.metaDataDotNetAssembly,application.metaDataDotNetType);
        return await iApplication.GetHealthCheckAsync(userRepository);
    }


    [Function(nameof(UpdateApplication))]
    public async Task<HttpResponseData> UpdateApplication(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "application/{id}")]
        HttpRequestData httpRequestData,
        string id)
    {
        return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
        {
            var updateApplicationRequest =
                await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<UpdateApplicationRequest>(httpRequestData);
            if (updateApplicationRequest == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");

            var application = await userRepository.GetApplicationByNameAsync(id);
            if (application == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Application");
            
            application.name=updateApplicationRequest.name!;
            application.schemaVersionNumber = application.schemaVersionNumber <= 2 ? 2 : application.schemaVersionNumber;
            application.isDefaultApplicationOnLogin = updateApplicationRequest.isDefaultApplicationOnLogin;
            application.ordinal = updateApplicationRequest.ordinal;
            
            await userRepository.UpdateApplicationAsync(id,application);

            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new UpdateApplicationResponse()
            {
                Name = application.name,
                ErrorMessage = null,
                IsOk = true
            });
        });
    }
    

    
    [Function(nameof(DeleteApplication))]
    public async Task<HttpResponseData> DeleteApplication(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "application/{id}")]
        HttpRequestData httpRequestData,
        string id)
    {
        return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
        {
            var application = await userRepository.GetApplicationByNameAsync(id);
            if (application == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Application");
            await userRepository.DeleteApplicationByNameAsync(id);
            return HttpResponseDataFactory.CreateForSuccessNoContent(httpRequestData);
        });
    }
    
    
    
    private string BuildPageUrl(string baseUrl, 
        string? containsText, 
        IEnumerable<string>? withinRoles, 
        string? continuationToken, 
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


    

    
}