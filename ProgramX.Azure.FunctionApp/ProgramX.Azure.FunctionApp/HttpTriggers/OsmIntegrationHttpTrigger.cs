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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using EmailMessage = ProgramX.Azure.FunctionApp.Model.EmailMessage;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class OsmIntegrationHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<ScoresLedgerHttpTrigger> _logger;
    private readonly IStorageClient? _storageClient;
    private readonly IEmailSender _emailSender;
    private readonly IUserRepository _userRepository;

 
    public OsmIntegrationHttpTrigger(ILogger<ScoresLedgerHttpTrigger> logger,
        IStorageClient? storageClient,
        IConfiguration configuration,
        IEmailSender emailSender,
        IUserRepository userRepository) : base(configuration)
    {
        _logger = logger;
        _storageClient = storageClient;
        _emailSender = emailSender;
        _userRepository = userRepository;
    }

    [Function(nameof(InitiateKeyExchange))]
    public async Task<HttpResponseData> InitiateKeyExchange(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/osm/initiatekeyexchange")]
        HttpRequestData httpRequestData)
    {
        using (_logger.BeginScope($"{nameof(OsmIntegrationHttpTrigger)}.{nameof(InitiateKeyExchange)}"))
        {
            var osmClientId = Configuration["Osm:ClientId"];
            var osmScopes = Configuration["Osm:Scopes"];
            var osmRedirectUri = GetRedirectUri();

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
            _logger.LogInformation("GETting URL {url}",url);
            // TODO GET the url
            
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
            {
                clientId = osmClientId,
                redirectUri = osmRedirectUri,
                scopes = osmScopes,
                url
            });
        }
    }

    private static string GetRedirectUri()
    {
        // TODO: Calculate redirectUri
        return "https://fa-programx.azurewebsites.net/api/v1/scouts/osm/completekeyexchange"; // this needs to be the other side within Azure Functions
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
                ["redirect_uri"] = GetRedirectUri()
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
    //
}