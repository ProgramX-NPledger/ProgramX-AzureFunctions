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
using ProgramX.Azure.FunctionApp.Model.Responses.Dtos;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class ApplicationsHttpTrigger(
    ILogger<ApplicationsHttpTrigger> logger,
    IConfiguration configuration,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IApplicationProvider applicationProvider,
    IServiceProvider serviceProvider)
    : AuthorisedHttpTriggerBase(configuration,logger)
{
    private readonly ILogger<ApplicationsHttpTrigger> _logger = logger;
    private readonly IApplicationProvider _applicationProvider = applicationProvider;


    [Function(nameof(GetApplication))]
    public async Task<HttpResponseData> GetApplication(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "applications/{name?}")] HttpRequestData httpRequestData,
        string? name)
    {
        return await RequiresAuthentication(httpRequestData, null, async (_, memberOfRoles) =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                var containsText = httpRequestData.Query["containsText"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["containsText"]!);
                var withinRoles = httpRequestData.Query["supportsRoles"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["supportsRoles"]!).Split(
                    [',']);
                
                var allApplications = _applicationProvider.GetAllApplications(new GetAllApplicationsCriteria());
                var filteredApplications = new List<IApplication>();

                // apply filters
                if (!string.IsNullOrWhiteSpace(containsText))
                {
                    filteredApplications.AddRange(allApplications.Where(a => 
                        a.GetApplicationMetaData().Name.Contains(containsText, StringComparison.OrdinalIgnoreCase) ||
                        (a.GetApplicationMetaData().Description ?? string.Empty).Contains(containsText, StringComparison.OrdinalIgnoreCase) ||
                        a.GetApplicationMetaData().FriendlyName.Contains(containsText, StringComparison.OrdinalIgnoreCase)
                                                  )
                    );
                }
                else
                {
                    filteredApplications.AddRange(allApplications);
                }
                
                if (withinRoles != null && withinRoles.Any())
                {
                    filteredApplications = filteredApplications.Where(a => withinRoles.Intersect(a.GetApplicationMetaData().RequiresRoleNames).Any()).ToList();
                }
                
                // remove applications user does not have access to
                filteredApplications = filteredApplications.Where(a => memberOfRoles.Intersect(a.GetApplicationMetaData().RequiresRoleNames).Any()).ToList();
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new GetApplicationsResponse()
                {
                    Applications = filteredApplications.Select(a => new ApplicationDto()
                    {
                        FriendlyName = a.GetApplicationMetaData().FriendlyName,
                        Description = a.GetApplicationMetaData().Description,
                        Name = a.GetApplicationMetaData().Name,
                        RequiresRoleNames = a.GetApplicationMetaData().RequiresRoleNames, 
                        TargetUrl = a.GetApplicationMetaData().TargetUrl, 
                        ImageUrl = a.GetApplicationMetaData().ImageUrl
                    })
                });
                
            }
            else
            {
                IApplication? application;
                application = _applicationProvider.GetApplication(name);
                if (application == null)
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Application");
                }
                
                var applicationDto = new ApplicationDto()
                {
                    Name = application.GetApplicationMetaData().Name,
                    FriendlyName = application.GetApplicationMetaData().FriendlyName,
                    Description = application.GetApplicationMetaData().Description,
                    ImageUrl = application.GetApplicationMetaData().ImageUrl,
                    TargetUrl = application.GetApplicationMetaData().TargetUrl,
                    RequiresRoleNames = application.GetApplicationMetaData().RequiresRoleNames
                };
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new GetApplicationResponse()
                {
                    Application = applicationDto
                });
            }
        });
    }
    
    
    [Function(nameof(GetApplicationHealthCheck))]
    public async Task<HttpResponseData> GetApplicationHealthCheck(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "applications/{name}/health")] HttpRequestData httpRequestData, string name)
    {
        return await RequiresAuthentication(httpRequestData, null, async (_, _) =>
        {
            // TODO: Potential security hole: may leak applications user does not have access to
            var application = _applicationProvider.GetApplication(name);
            if (application == null)
            {
                return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Application");
            }
    
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
    
         
        });
    
    }
    
//     
//     [Function(nameof(FixApplication))]
//     public async Task<HttpResponseData> FixApplication(
//         [HttpTrigger(AuthorizationLevel.Function, "post", Route = "application/{name}/health/fix")] HttpRequestData httpRequestData, string name)
//     {
//         return await RequiresAuthentication(httpRequestData, null, async (_, _) =>
//         {
//             var applicationLoader = new ApplicationLoader(Configuration, serviceProvider);
//             var applicationNames = applicationLoader.GetApplicationNames();
//             if (!applicationNames.Contains(name))
//             {
//                 return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,
//                     $"No application found with name {name}");
//             }
//
//             try
//             {
//                 var application = applicationLoader.LoadApplication(name);
//
//                 var fixApplicationHealthCheckResults = new List<FixApplicationHealthCheckResult>();
//                 var healthChecks = application.GetHealthChecks();
//                 foreach (var healthCheck in healthChecks)
//                 {
//                     var healthCheckResult = await healthCheck.CheckHealthAsync();
//                     var fixApplicationHealthCheckResult = await healthCheck.FixHealthAsync(healthCheckResult);
//
//                     fixApplicationHealthCheckResults.Add(fixApplicationHealthCheckResult);
//                 }
//
//                 return await HttpResponseDataFactory.CreateForSuccess(httpRequestData,
//                     new FixApplicationResponse()
//                     {
//                         Items = fixApplicationHealthCheckResults.Select(q => new FixApplicationByHealthCheckResult
//                         {
//                             Name = fixApplicationHealthCheckResults.First().Name,
//                             IsSuccess = fixApplicationHealthCheckResults.All(q => q.IsSuccess),
//                             Messages = fixApplicationHealthCheckResults.SelectMany(q => q.Messages)
//                         })
//                     });
//             }
//             catch (InvalidOperationException ioe)
//             {
//                 return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,
//                     $"Failed to load Application with name {name} due to {ioe.Message}");
//             }
//             
//         });
//
//     }
//
//     /// <summary>
//     /// Based on the authenticated user, get the applications that can be checked for health.
//     /// </summary>
//     /// <param name="httpRequestData"></param>
//     /// <returns></returns>
//     [Function(nameof(GetApplicationsForHealthCheck))]
//     public async Task<HttpResponseData> GetApplicationsForHealthCheck(
//         [HttpTrigger(AuthorizationLevel.Function, "get", Route = "application/health")] HttpRequestData httpRequestData)
//     {
//         return await RequiresAuthentication(httpRequestData, null, async (_, roles) =>
//         {
//             var baseUrl =
//                 $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}/application".Replace("/application/health", "");
//
//             // if user is an admin return all applications
//             if (roles.Contains("admin"))
//             {
//                 var applicationLoader = new ApplicationLoader(Configuration, serviceProvider);
//                 var applications = applicationLoader.GetApplicationNames().ToList();
//                 List<ApplicationMetaData> applicationsMetaData = new List<ApplicationMetaData>();
//
//                 var response = new GetApplicationsForHealthCheckResponse()
//                 {
//                     TimeStamp = DateTime.UtcNow,
//                     IsElevated = true,
//                     HealthCheckServices = new List<ApplicationHealthCheckService>()
//                 };
//                 
//                 applications.ForEach(q =>
//                 {
//                     IApplication? application = null;
//                     try
//                     {
//                         application = applicationLoader.LoadApplication(q);
//                     }
//                     catch (InvalidOperationException ioex)
//                     {
//                         // TODO: Log it
//                         response.HealthCheckServices.Add(new ApplicationHealthCheckService()
//                         {
//                             Name = q,
//                             FriendlyName = q,
//                             IsLoaded = false,
//                             Messages = new string[] { ioex.Message }
//                         });
//                     }
//                     
//                     if (application != null)
//                     {
//                         try
//                         {
//                             var applicationMetaData = application.GetApplicationMetaData();
//                             response.HealthCheckServices.Add(new ApplicationHealthCheckService()
//                             {
//                                 Name = applicationMetaData.Name,
//                                 FriendlyName = applicationMetaData.FriendlyName,
//                                 Url = $"{baseUrl}/{applicationMetaData.Name}/health",
//                                 IsLoaded = true
//                             });
//                         }
//                         catch (Exception e)
//                         {
//                             // TODO: Log it
//                             response.HealthCheckServices.Add(new ApplicationHealthCheckService()
//                             {
//                                 Name = application.GetType().Name,
//                                 FriendlyName = application.GetType().Name,
//                                 IsLoaded = false,
//                                 Messages = new string[] { e.Message }
//                             });
//                         }
//                     }                
//                 });
//                 return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, response);
//             }
//             else
//             {
//                 var allRoles = await roleRepository.GetRolesAsync(new GetRolesCriteria());
//                 var currentRoles = allRoles.Items.Where(r => roles.Contains(r.RoleName));
// //                var applicationsWithinRoles = currentRoles.SelectMany(r => r.applications);
//                 return await HttpResponseDataFactory.CreateForSuccess(httpRequestData,
//                     new GetApplicationsForHealthCheckResponse()
//                     {
//                         TimeStamp = DateTime.UtcNow,
//                         IsElevated = false,
//                         HealthCheckServices = Enumerable.Empty<ApplicationHealthCheckService>().ToList()
//                         // TODO applicationsWithinRoles
//                         // applicationsWithinRoles.Select(q => new ApplicationHealthCheckService()
//                         // {
//                         //     Name = q.name,
//                         //     Url = $"{baseUrl}/{q.name}/health",
//                         // }).ToList()
//                     });
//             }
//         });
//     }
    


    

    

    
}