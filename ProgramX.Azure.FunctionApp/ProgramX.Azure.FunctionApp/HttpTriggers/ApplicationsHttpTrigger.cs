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
                var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]);
                var containsText = httpRequestData.Query["containsText"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["containsText"]);
                var withinRoles = httpRequestData.Query["withinRoles"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["withinRoles"]).Split(new [] {','});

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
    
    
    /// <summary>
    /// Create an Application.
    /// </summary>
    /// <param name="httpRequestData"></param>
    /// <returns></returns>
    /// <response code="201">Application created.</response>
    /// <response code="400">Invalid request body.</response>
    /// <response code="409">Application already exists.</response>
    /// <response code="500">Internal server error.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="403">Forbidden.</response>
    [Function(nameof(CreateApplication))]
    public async Task<HttpResponseData> CreateApplication(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "application")] HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, null,  async (_, _) =>
        {
            var createApplicationRequest =
                await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<CreateApplicationRequest>(httpRequestData);
            if (createApplicationRequest == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");

            var existingApplication = await userRepository.GetApplicationByNameAsync(createApplicationRequest.name);
            if (existingApplication != null) return await HttpResponseDataFactory.CreateForConflict(httpRequestData, "Application already exists");
            
            var newApplication = new Application()
            {
                name = createApplicationRequest.name,
                description = createApplicationRequest.description,
                schemaVersionNumber = 1,
                targetUrl = createApplicationRequest.targetUrl,
                imageUrl = createApplicationRequest.imageUrl,
                isDefaultApplicationOnLogin = createApplicationRequest.isDefaultApplicationOnLogin,
                ordinal = createApplicationRequest.ordinal,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow,
            };

            try
            {
                await userRepository.CreateApplicationAsync(newApplication, createApplicationRequest.addToRoles);
            }
            catch (RepositoryException e)
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, e.Message);           
            }

            return await HttpResponseDataFactory.CreateForCreated(httpRequestData, newApplication, "application", newApplication.name);    
        });
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
            application.description = updateApplicationRequest.description!;
            application.schemaVersionNumber = application.schemaVersionNumber <= 1 ? 1 : application.schemaVersionNumber;
            application.targetUrl = updateApplicationRequest.targetUrl;
            application.imageUrl = updateApplicationRequest.imageUrl;
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


    

    
}