using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Constants;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;
using ProgramX.Azure.FunctionApp.Osm;
using ProgramX.Azure.FunctionApp.Osm.Model.Criteria;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using EmailMessage = ProgramX.Azure.FunctionApp.Model.EmailMessage;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class OsmIntegrationHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<OsmIntegrationHttpTrigger> _logger;
    private readonly IOsmClient _osmClient;


    public OsmIntegrationHttpTrigger(ILogger<OsmIntegrationHttpTrigger> logger,
        IConfiguration configuration,
        IOsmClient osmClient
        ) : base(configuration,logger)
    {
        _logger = logger;
        _osmClient = osmClient;
    }

    [Function(nameof(InitiateKeyExchange))]
    public async Task<HttpResponseData> InitiateKeyExchange(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/osm/initiatekeyexchange")]
        HttpRequestData httpRequestData)
    {
        using (_logger.BeginScope($"{nameof(OsmIntegrationHttpTrigger)}.{nameof(InitiateKeyExchange)}"))
        {
            var osmClientId = Configuration["Osm:ClientId"];
            if (string.IsNullOrWhiteSpace(osmClientId))
            {
                _logger.LogError("Configuration Osm:ClientId is not set.");
                return await HttpResponseDataFactory.CreateForServerError(httpRequestData,
                    "No OSM client ID configured");
            }
            var osmScopes = Configuration["Osm:Scopes"];
            if (string.IsNullOrWhiteSpace(osmScopes))
            {
                _logger.LogError("Configuration Osm:Scopes is not set.");
                return await HttpResponseDataFactory.CreateForServerError(httpRequestData,
                    "No OSM scopes configured");
            }
            var osmRedirectUri = GetRedirectUri(httpRequestData);

            _logger.LogInformation(
                "OSM authentication configuration: clientId={clientId}, redirectUrl={redirectUri}, scopes={scopes}",
                osmClientId, osmRedirectUri, osmScopes);
            
            var osmAuthUrl = "https://www.onlinescoutmanager.co.uk/oauth/authorize";

            // Build authorization URL
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["response_type"] = "code";
            query["client_id"] = osmClientId;
            query["redirect_uri"] = osmRedirectUri;
            query["scope"] = osmScopes;

            var url = $"{osmAuthUrl}?{query}";
            _logger.LogInformation("Browse to URL {url}",url);
            
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
            {
                clientId = osmClientId,
                redirectUri = osmRedirectUri,
                scopes = osmScopes,
                url,
                message = "Navigate to the URL to authenticate with OSM"
            });
        }
    }

    private static string GetRedirectUri(HttpRequestData httpRequestData)
    {
        return $"{httpRequestData.Url.Scheme}s://{httpRequestData.Url.Authority}/api/v1/scouts/osm/completekeyexchange";
    }

    [Function(nameof(CompleteKeyExchange))]
    public async Task<HttpResponseData> CompleteKeyExchange(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/osm/completekeyexchange")]
        HttpRequestData httpRequestData)
    {
        using (_logger.BeginScope($"{nameof(OsmIntegrationHttpTrigger)}.{nameof(CompleteKeyExchange)}"))
        {
            // the request will have a code
            var code = httpRequestData.Query["code"];
            if (code == null)
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,  "No code was returned from OSM");
            }

            // Exchange code for tokens
            var postData = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["client_id"] = Configuration["Osm:ClientId"],
                ["client_secret"] = Configuration["Osm:ClientSecret"],
                ["redirect_uri"] = GetRedirectUri(httpRequestData)
            };

            var httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(postData);

            var osmTokenUrl = "https://www.onlinescoutmanager.co.uk/oauth/token";
            
            var tokenResponse = await httpClient.PostAsync(osmTokenUrl, content);
            string json = await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Token exchange failed: {json}", json);
                return await HttpResponseDataFactory.CreateForServerError(httpRequestData, json);
            }

            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, json);
        }
    }
    //
    // [Function(nameof(GetFlexiRecords))]
    // public async Task<HttpResponseData> GetFlexiRecords(
    //     [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/osm/flexirecords")]
    //     HttpRequestData httpRequestData)
    // {
    //     return await RequiresAuthentication(httpRequestData, "osm-reader", async (_, _) =>
    //     {
    //         // email=nathan%40programx.co.uk&password=Cruelty7-Rebuild-Phoney&device_id=a8f0c64b-3520-4c95-8f0e-2d3b9b326a8d&seen=internal+2
    //         
    //         // TODO: Go to OSM and get all flexi records
    //         //
    //         // if (id == null)
    //         // {
    //         //     var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]!);
    //         //     var containsText = httpRequestData.Query["containsText"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["containsText"]!);
    //         //     var withRoles = httpRequestData.Query["withRoles"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["withRoles"]!).Split(new [] {','});
    //         //     var hasAccessToApplications = httpRequestData.Query["hasAccessToApplications"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["hasAccessToApplications"]!).Split(new [] {','});
    //         //
    //         //     var sortByColumn = httpRequestData.Query["sortBy"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["sortBy"]!);
    //         //     var offset = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["offset"]) ??
    //         //                  0;
    //         //     var itemsPerPage = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["itemsPerPage"]) ?? PagingConstants.ItemsPerPage;
    //         //     
    //         //     var users = await _userRepository.GetUsersAsync(new GetUsersCriteria()
    //         //     {
    //         //         HasAccessToApplications = hasAccessToApplications,
    //         //         WithRoles = withRoles,
    //         //         ContainingText = containsText
    //         //     }, new PagedCriteria()
    //         //     {
    //         //         ItemsPerPage = itemsPerPage,
    //         //         Offset = offset
    //         //     });
    //         //     
    //         //     var baseUrl =
    //         //         $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";
    //         //     
    //         //     var pageUrls = CalculatePageUrls((IPagedResult<User>)users,
    //         //         baseUrl,
    //         //         containsText,
    //         //         withRoles,
    //         //         hasAccessToApplications,
    //         //         continuationToken, 
    //         //         offset,
    //         //         itemsPerPage);
    //         //     
    //         //     return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<User>((IPagedResult<User>)users,pageUrls));
    //         // }
    //         // else
    //         // {
    //         //     var user = await _userRepository.GetUserByIdAsync(id);
    //         //     if (user==null)
    //         //     {
    //         //         return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
    //         //     }
    //         //     
    //         //     List<Application> applications = user.roles.SelectMany(q=>q.applications).GroupBy(g=>g.name).Select(q=>q.First()).ToList();
    //         //     
    //         //     return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
    //         //     {
    //         //         user,
    //         //         applications,
    //         //         profilePhotoBase64 = string.Empty
    //         //     });
    //         // }
    //     });
    //     
    // }   
    //
    // [Function(nameof(GetMeetings))]
    // public async Task<HttpResponseData> GetMeetings(
    //     [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/osm/meetings")] HttpRequestData httpRequestData)
    // {
    //     return await RequiresAuthentication(httpRequestData, null, async (userName, _) =>
    //     {
    //         // TODO: Go to OSM and get all meetings between time period
    //         //
    //         // if (id == null)
    //         // {
    //         //     var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]!);
    //         //     var containsText = httpRequestData.Query["containsText"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["containsText"]!);
    //         //     var withRoles = httpRequestData.Query["withRoles"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["withRoles"]!).Split(new [] {','});
    //         //     var hasAccessToApplications = httpRequestData.Query["hasAccessToApplications"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["hasAccessToApplications"]!).Split(new [] {','});
    //         //
    //         //     var sortByColumn = httpRequestData.Query["sortBy"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["sortBy"]!);
    //         //     var offset = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["offset"]) ??
    //         //                  0;
    //         //     var itemsPerPage = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["itemsPerPage"]) ?? PagingConstants.ItemsPerPage;
    //         //     
    //         //     var users = await _userRepository.GetUsersAsync(new GetUsersCriteria()
    //         //     {
    //         //         HasAccessToApplications = hasAccessToApplications,
    //         //         WithRoles = withRoles,
    //         //         ContainingText = containsText
    //         //     }, new PagedCriteria()
    //         //     {
    //         //         ItemsPerPage = itemsPerPage,
    //         //         Offset = offset
    //         //     });
    //         //     
    //         //     var baseUrl =
    //         //         $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";
    //         //     
    //         //     var pageUrls = CalculatePageUrls((IPagedResult<User>)users,
    //         //         baseUrl,
    //         //         containsText,
    //         //         withRoles,
    //         //         hasAccessToApplications,
    //         //         continuationToken, 
    //         //         offset,
    //         //         itemsPerPage);
    //         //     
    //         //     return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<User>((IPagedResult<User>)users,pageUrls));
    //         // }
    //         // else
    //         // {
    //         //     var user = await _userRepository.GetUserByIdAsync(id);
    //         //     if (user==null)
    //         //     {
    //         //         return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
    //         //     }
    //         //     
    //         //     List<Application> applications = user.roles.SelectMany(q=>q.applications).GroupBy(g=>g.name).Select(q=>q.First()).ToList();
    //         //     
    //         //     return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
    //         //     {
    //         //         user,
    //         //         applications,
    //         //         profilePhotoBase64 = string.Empty
    //         //     });
    //         // }
    //     });
    // }
    //
    //
    //
    
    [Function(nameof(GetMembers))]
    public async Task<HttpResponseData> GetMembers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/osm/members")] HttpRequestData httpRequestData,
        int termId,
        int? sectionId)
    { 
        return await RequiresAuthentication(httpRequestData, ["admin","reader"], async (userName, _) =>
        {
            var terms = await _osmClient.GetMembers(new GetMembersCriteria()
            {
                TermId = termId,
                SectionId = sectionId
            });
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, terms);
        });
    }

     
     [Function(nameof(GetTerms))]
     public async Task<HttpResponseData> GetTerms(
         [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/osm/terms")] HttpRequestData httpRequestData,
         int? sectionId)
     { 
         return await RequiresAuthentication(httpRequestData, ["admin","reader"], async (userName, _) =>
         {
             var terms = await _osmClient.GetTerms(new GetTermsCriteria()
             {
                 SectionId = sectionId
             });
             return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, terms);
         });
     }
     
}