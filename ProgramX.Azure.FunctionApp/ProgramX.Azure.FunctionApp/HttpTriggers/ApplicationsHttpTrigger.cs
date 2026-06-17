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
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IServiceProvider serviceProvider)
    : AuthorisedHttpTriggerBase(configuration,logger)
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
                // if the application name is "health" we have a route overlap, just call the intended route
                // it does mean we cannot have any applications with the name "health"
                if (name.Equals("health", StringComparison.CurrentCultureIgnoreCase)) return await GetApplicationsForHealthCheck(httpRequestData);
                
                var application = await userRepository.GetApplicationByNameAsync(name);
                if (application==null)
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Application");
                }

                // var withinRoles = userRepository.GetRolesAsync(new GetRolesCriteria()
                // {
                //     UsedInApplicationNames = [name]
                // });
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
                {
                    application,
                    //usedInRoles = withinRoles.Result.Items
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
    
    
    [Function(nameof(GetApplicationHealthCheck))]
    public async Task<HttpResponseData> GetApplicationHealthCheck(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "application/{name}/health")] HttpRequestData httpRequestData, string name)
    {
        return await RequiresAuthentication(httpRequestData, null, async (_, _) =>
        {
            // need to get all applications, including those not in a role to ensure that the application definitely exists
            var applicationLoader = new ApplicationLoader(Configuration, serviceProvider);
            var applicationNames = applicationLoader.GetApplicationNames();
            if (!applicationNames.Contains(name))
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,
                    $"No application found with name {name}");
            }

            try
            {
                var application = applicationLoader.LoadApplication(name);

                var healthCheckResults = new List<HealthCheckResult>();
                var healthChecks = application.GetHealthChecks();
                foreach (var healthCheck in healthChecks)
                {
                    healthCheckResults.Add(await healthCheck.CheckHealthAsync());
                }

                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData,
                    new GetHealthCheckForApplicationResponse()
                    {
                        Name = name,
                        IsHealthy = healthCheckResults.All(q => q.IsHealthy),
                        Message = healthCheckResults.All(q => q.IsHealthy) ? "The Application is healthy" : "The Application is unhealthy",
                        TimeStamp = DateTime.UtcNow,
                        Items = healthCheckResults
                    });

            }
            catch (InvalidOperationException ioe)
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,
                    $"Failed to load Application with name {name} due to {ioe.Message}");
            }
            
        });

    }
    
    
    [Function(nameof(FixApplication))]
    public async Task<HttpResponseData> FixApplication(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "application/{name}/health/fix")] HttpRequestData httpRequestData, string name)
    {
        return await RequiresAuthentication(httpRequestData, null, async (_, _) =>
        {
            var applicationLoader = new ApplicationLoader(Configuration, serviceProvider);
            var applicationNames = applicationLoader.GetApplicationNames();
            if (!applicationNames.Contains(name))
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,
                    $"No application found with name {name}");
            }

            try
            {
                var application = applicationLoader.LoadApplication(name);

                var fixApplicationHealthCheckResults = new List<FixApplicationHealthCheckResult>();
                var healthChecks = application.GetHealthChecks();
                foreach (var healthCheck in healthChecks)
                {
                    var healthCheckResult = await healthCheck.CheckHealthAsync();
                    var fixApplicationHealthCheckResult = await healthCheck.FixHealthAsync(healthCheckResult);

                    fixApplicationHealthCheckResults.Add(fixApplicationHealthCheckResult);
                }

                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData,
                    new FixApplicationResponse()
                    {
                        Items = fixApplicationHealthCheckResults.Select(q => new FixApplicationByHealthCheckResult
                        {
                            Name = fixApplicationHealthCheckResults.First().Name,
                            IsSuccess = fixApplicationHealthCheckResults.All(q => q.IsSuccess),
                            Messages = fixApplicationHealthCheckResults.SelectMany(q => q.Messages)
                        })
                    });
            }
            catch (InvalidOperationException ioe)
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,
                    $"Failed to load Application with name {name} due to {ioe.Message}");
            }
            
        });

    }

    /// <summary>
    /// Based on the authenticated user, get the applications that can be checked for health.
    /// </summary>
    /// <param name="httpRequestData"></param>
    /// <returns></returns>
    [Function(nameof(GetApplicationsForHealthCheck))]
    public async Task<HttpResponseData> GetApplicationsForHealthCheck(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "application/health")] HttpRequestData httpRequestData)
    {
        return await RequiresAuthentication(httpRequestData, null, async (_, roles) =>
        {
            var baseUrl =
                $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}/application".Replace("/application/health", "");

            // if user is an admin return all applications
            if (roles.Contains("admin"))
            {
                var applicationLoader = new ApplicationLoader(Configuration, serviceProvider);
                var applications = applicationLoader.GetApplicationNames().ToList();
                List<ApplicationMetaData> applicationsMetaData = new List<ApplicationMetaData>();

                var response = new GetApplicationsForHealthCheckResponse()
                {
                    TimeStamp = DateTime.UtcNow,
                    IsElevated = true,
                    HealthCheckServices = new List<ApplicationHealthCheckService>()
                };
                
                applications.ForEach(q =>
                {
                    IApplication? application = null;
                    try
                    {
                        application = applicationLoader.LoadApplication(q);
                    }
                    catch (InvalidOperationException ioex)
                    {
                        // TODO: Log it
                        response.HealthCheckServices.Add(new ApplicationHealthCheckService()
                        {
                            Name = q,
                            FriendlyName = q,
                            IsLoaded = false,
                            Messages = new string[] { ioex.Message }
                        });
                    }
                    
                    if (application != null)
                    {
                        try
                        {
                            var applicationMetaData = application.GetApplicationMetaData();
                            response.HealthCheckServices.Add(new ApplicationHealthCheckService()
                            {
                                Name = applicationMetaData.Name,
                                FriendlyName = applicationMetaData.FriendlyName,
                                Url = $"{baseUrl}/{applicationMetaData.Name}/health",
                                IsLoaded = true
                            });
                        }
                        catch (Exception e)
                        {
                            // TODO: Log it
                            response.HealthCheckServices.Add(new ApplicationHealthCheckService()
                            {
                                Name = application.GetType().Name,
                                FriendlyName = application.GetType().Name,
                                IsLoaded = false,
                                Messages = new string[] { e.Message }
                            });
                        }
                    }                
                });
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, response);
            }
            else
            {
                var allRoles = await roleRepository.GetRolesAsync(new GetRolesCriteria());
                var currentRoles = allRoles.Items.Where(r => roles.Contains(r.RoleName));
//                var applicationsWithinRoles = currentRoles.SelectMany(r => r.applications);
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData,
                    new GetApplicationsForHealthCheckResponse()
                    {
                        TimeStamp = DateTime.UtcNow,
                        IsElevated = false,
                        HealthCheckServices = Enumerable.Empty<ApplicationHealthCheckService>().ToList()
                        // TODO applicationsWithinRoles
                        // applicationsWithinRoles.Select(q => new ApplicationHealthCheckService()
                        // {
                        //     Name = q.name,
                        //     Url = $"{baseUrl}/{q.name}/health",
                        // }).ToList()
                    });
            }
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