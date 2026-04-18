using System.Diagnostics;
using System.Globalization;
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
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Newtonsoft.Json;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Constants;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.DTOs.Osm;
using ProgramX.Azure.FunctionApp.Model.DTOs.Osm.Response;
using ProgramX.Azure.FunctionApp.Model.Osm;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;
using ProgramX.Azure.FunctionApp.Osm;
using ProgramX.Azure.FunctionApp.Osm.Constants;
using ProgramX.Azure.FunctionApp.Osm.Helpers;
using ProgramX.Azure.FunctionApp.Osm.Model;
using ProgramX.Azure.FunctionApp.Osm.Model.Criteria;
using ProgramX.Azure.FunctionApp.Osm.Model.Osm.Responses;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using EmailMessage = ProgramX.Azure.FunctionApp.Model.EmailMessage;
using GetMembersResponse = ProgramX.Azure.FunctionApp.Model.DTOs.Osm.Response.GetMembersResponse;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class OsmIntegrationHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<OsmIntegrationHttpTrigger> _logger;
    private readonly IOsmClient _osmClient;
    private readonly IIntegrationRepository _integrationRepository;


    public OsmIntegrationHttpTrigger(ILogger<OsmIntegrationHttpTrigger> logger,
        IConfiguration configuration,
        IOsmClient osmClient,
        IIntegrationRepository integrationRepository
        ) : base(configuration,logger)
    {
        _logger = logger;
        _osmClient = osmClient;
        _integrationRepository = integrationRepository;
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

            osmScopes = osmScopes.Replace(":", "%3a").Replace(" ", "%20");
            
            var keyCompletionEndpoint = Environment.GetEnvironmentVariable("OsmOAuth2KeyCompletion");

            _logger.LogInformation(
                "OSM authentication configuration: clientId={clientId}, redirectUrl={redirectUri}, scopes={scopes}",
                osmClientId, keyCompletionEndpoint, osmScopes);
            
            var osmAuthUrl = "https://www.onlinescoutmanager.co.uk/oauth/authorize";

            // Build authorization URL
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["response_type"] = "code";
            query["client_id"] = osmClientId;
            query["redirect_uri"] = keyCompletionEndpoint;
            query["scope"] = osmScopes;

            var url = $"{osmAuthUrl}?response_type=code&client_id={osmClientId}&redirect_uri={keyCompletionEndpoint}&scope={osmScopes}";
            _logger.LogInformation("Browse to URL {url}",url);
            
            return await HttpResponseDataFactory.CreateForSuccessAsString(httpRequestData, url);
        }
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
            string jsonAsString = await tokenResponse.Content.ReadAsStringAsync();
            
            // save tokens
            var tokens = System.Text.Json.JsonSerializer.Deserialize<OsmTokenRefreshResponse>(jsonAsString);
            if (tokens.IsError)
            {
                throw new OsmException(tokens);
            }
            
            await _integrationRepository.SetBearerAndRefreshTokensAsync(ServiceConstants.ServiceName, Configuration["Osm:ClientId"], tokens.AccessToken, tokens.RefreshToken);
            
            

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Token exchange failed: {json}", jsonAsString);
                return await HttpResponseDataFactory.CreateForServerError(httpRequestData, jsonAsString);
            }

            // return enough
            string responseBody = @$"
<html>
    <head>
        <title>OSM Authentication Complete</title>
    </head>
    <body>
<script ""text/javascript"">
            window.opener?.postMessage(
            {{ type: 'LOGIN_COMPLETE', data: {jsonAsString} }},
            window.location.origin
                );

            window.close();
</script>
        <p>OSM authentication complete. You can close this window.</p>
    </body>
</html>
";
            
            
            return await HttpResponseDataFactory.CreateForSuccessAsHtml(httpRequestData, responseBody);
        }
    }
    
    private static string GetRedirectUri(HttpRequestData httpRequestData)
    {
        return $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}/api/v1/scouts/osm/completekeyexchange";
    }
 
    [Function(nameof(GetMembers))]
    public async Task<HttpResponseData> GetMembers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/osm/members")] HttpRequestData httpRequestData,
        int termId,
        int? sectionId)
    { 
        return await RequiresAuthentication(httpRequestData, ["admin","scouts-reader"], async (_, _) =>
        {
            var members = (await _osmClient.GetMembersAsync(new GetMembersCriteria()
            {
                TermId = termId,
                SectionId = sectionId
            })).ToList();
            
            var getMembersResponse = new GetMembersResponse()
            {
                Items = members.Select(q => new MemberDto()
                {
                    Age = q.Age,
                    FirstName = q.FirstName,
                    LastName = q.LastName,
                    PatrolRoleLevel = q.PatrolRoleLevel,
                    OsmScoutId = q.OsmScoutId,
                    StartDate = q.StartDate,
                    EndDate = q.EndDate,
                    IsActive = q.IsActive,
                    FullName = q.FullName,
                    OsmSectionId = q.OsmSectionId,
                    OsmPatrolId = q.OsmPatrolId,
                    PatrolNameAndLevel = q.PatrolNameAndLevel,
                    HasInvitations = q.HasInvitations,
                    PatrolName = q.PatrolName,
                    PhotoId = q.PhotoId
                }).ToList()
            };
            
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, getMembersResponse);
        });
    }

  

    [Function(nameof(GetMeetings))]
    public async Task<HttpResponseData> GetMeetings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/osm/meetings")] HttpRequestData httpRequestData,
        int termId,
        int? sectionId,
        bool? hasOutstandingRequiredParents,
        bool? hasPrimaryLeader,
        string? keywords,
        string? onOrAfter,
        string? onOrBefore,
        string? sortBy)
    { 
        return await RequiresAuthentication(httpRequestData, ["admin","reader"], async (userName, _) =>
        {
            var criteria = new GetMeetingsCriteria()
            {
                TermId = termId,
                SectionId = sectionId,
                HasOutstandingRequiredParents = hasOutstandingRequiredParents,
                HasPrimaryLeader = hasPrimaryLeader
            };
            if (!string.IsNullOrWhiteSpace(keywords)) criteria.Keywords = keywords.Split(',').Select(q=>q.Trim()).ToList();
            if (!string.IsNullOrWhiteSpace(onOrAfter))
            {
                DateOnly parsedDateOnly;
                if (DateOnly.TryParse(onOrAfter,out parsedDateOnly)) criteria.OccursOnOrAfter = parsedDateOnly;
            }
            if (!string.IsNullOrWhiteSpace(onOrBefore))
            {
                DateOnly parsedDateOnly;
                if (DateOnly.TryParse(onOrBefore,out parsedDateOnly)) criteria.OccursOnOrBefore = parsedDateOnly;
            }
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                GetMeetingsSortBy getMeetingsSortBy;
                if (Enum.TryParse(sortBy,true,out getMeetingsSortBy)) criteria.SortBy = getMeetingsSortBy;
            }
            
            var terms = await _osmClient.GetMeetingsAsync(criteria);
            
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, terms);
        });
    }

     
     [Function(nameof(GetTerms))]
     public async Task<HttpResponseData> GetTerms(
         [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/osm/terms")] HttpRequestData httpRequestData,
         int? sectionId)
     { 
         return await RequiresAuthentication(httpRequestData, ["admin","scouts-reader"], async (_, _) =>
         {
             var terms = await _osmClient.GetTermsAsync(new GetTermsCriteria()
             {
                 SectionId = sectionId
             });
             var getTermsResponse = new GetTermsResponse()
             {
                 Items = terms.Select(q => new TermDto()
                 {
                     Name = q.Name,
                     StartDate = q.StartDate,
                     EndDate = q.EndDate,
                     OsmTermId = q.OsmTermId,
                     MasterTerm = q.MasterTerm,
                     IsPast = q.IsPast,
                     SectionId = q.SectionId,
                     IsCurrent = q.IsCurrent
                 }).ToList()
             };
             
             return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, getTermsResponse);
         });
     }
    
     
     [Function(nameof(GetAttendance))]
     public async Task<HttpResponseData> GetAttendance(
         [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/osm/attendance")] HttpRequestData httpRequestData,
         int? sectionId,
         string? onOrAfter,
         string? onOrBefore
         )
     { 
         return await RequiresAuthentication(httpRequestData, ["admin","reader"], async (_, _) =>
         {
             // if term isn't provided, we need to do multiple calls to get all terms to get between dates
             var attendances = GetAttendancesBetweenDatesAsync(onOrAfter, onOrBefore, sectionId);
             
             return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, attendances);
         });
     }

     private async Task<IEnumerable<Attendance>> GetAttendancesBetweenDatesAsync(string? onOrAfter, string? onOrBefore, int? sectionId, int? memberId = null)
     {
        var attendances = new List<Attendance>();

        // get the Terms that include the period
        var getForTermIds = new List<int>();

        var dateRange = Translation.TranslateStringsToDateRange(onOrAfter,onOrBefore);
        getForTermIds = (await GetTermIdsForPeriod(sectionId, dateRange.OnOrAfter, dateRange.OnOrBefore)).ToList();

        foreach (var termId in getForTermIds)
        {
            var getAttendanceCriteria = new GetAttendanceCriteria()
            {
                SectionId = sectionId, 
                TermId = termId,
                MemberId = memberId
            };
             
            if (!string.IsNullOrWhiteSpace(onOrAfter))
            { 
                DateOnly parsedDateOnly;
                if (DateOnly.TryParse(onOrAfter,out parsedDateOnly)) getAttendanceCriteria.OnOrAfter = parsedDateOnly;
            }
         
            if (!string.IsNullOrWhiteSpace(onOrBefore))
            {
                DateOnly parsedDateOnly;
                if (DateOnly.TryParse(onOrBefore,out parsedDateOnly)) getAttendanceCriteria.OnOrBefore = parsedDateOnly;
            }
             
            var attendanceForTerm = await _osmClient.GetAttendanceAsync(getAttendanceCriteria);
            attendances.AddRange(attendanceForTerm);
        }

        return attendances;
     }

     private async Task<IEnumerable<int>> GetTermIdsForPeriod(int? sectionId, DateOnly? onOrAfter, DateOnly? onOrBefore)
     {
         List<int> getForTermIds;
         // get all terms between dates
         var getTermsCriteria = new GetTermsCriteria()
         {
             SectionId = sectionId,
             StartsOnOrAfter = onOrAfter,
             EndsOnOrBefore = onOrBefore
         };
         
         var terms = await _osmClient.GetTermsAsync(getTermsCriteria);
         getForTermIds = terms.Select(t => t.OsmTermId).ToList();
         return getForTermIds;
     }


     [Function(nameof(GetAttendanceOverPeriodReport))]
     public async Task<HttpResponseData> GetAttendanceOverPeriodReport(
         [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/osm/report/attendance-over-term")] HttpRequestData httpRequestData,
         string onOrAfter,
         string onOrBefore,
         int? sectionId,
         int? memberId,
         int intervalInDays = 7
         )
     { 
         return await RequiresAuthentication(httpRequestData, ["admin","reader"], async (userName, _) =>
         {
             if (!DateOnly.TryParse(onOrAfter,out DateOnly parsedOnOrAfter)) 
                 return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid onOrAfter date");
             if (!DateOnly.TryParse(onOrBefore,out DateOnly parsedOnOrBefore)) 
                 return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid onOrBefore date");

             var attendance = await GetAttendancesBetweenDatesAsync(onOrAfter, onOrBefore, sectionId, memberId);
             
            var dates = new List<DateOnly>();
            var datePtr = parsedOnOrAfter;
            do
            {
                dates.Add(datePtr);
                datePtr = datePtr.AddDays(intervalInDays);
            } while (datePtr <= parsedOnOrBefore.AddDays(intervalInDays));
            
            // these will be implicitly sorted by Date

            if (memberId.HasValue)
            {
                var memberAttendance = dates.ToDictionary(date=>date,date=>attendance.Count(q=>q.AttendanceOverTerm.ContainsKey(date) && q.IsActive));
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new 
                {
                    MemberAttendance = memberAttendance,
                    // PercentageMemberAttendanceChange = ((memberAttendance.LastOrDefault(q=>q.Value>0).Value - memberAttendance.FirstOrDefault(q=>q.Value>0).Value) /
                    //                                     memberAttendance.FirstOrDefault(q=>q.Value>0).Value) * 100,
                });
            }
            else
            {
                var scouts = dates.ToDictionary(date=>date,date=>attendance.Count(q=>q.AttendanceOverTerm.ContainsKey(date) && q.IsActive && q.OsmPatrolId >= 1));
                var leaders = dates.ToDictionary(date=>date,date=>attendance.Count(q=>q.AttendanceOverTerm.ContainsKey(date) && q.IsActive && q.OsmPatrolId < 0));
            
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new 
                {
                    ScoutsAttendance = scouts,
                    LeadersAttendance = leaders,
                    // PercentageScoutsAttendanceChange = ((scouts.LastOrDefault(q=>q.Value>0).Value - scouts.FirstOrDefault(q=>q.Value>0).Value) /
                    //                                     scouts.FirstOrDefault(q=>q.Value>0).Value) * 100,
                    // PercentageLeadersAttendanceChange = ((leaders.LastOrDefault(q=>q.Value>0).Value - leaders.FirstOrDefault(q=>q.Value>0).Value) /
                    //                                      leaders.FirstOrDefault(q=>q.Value>0).Value) * 100,
                });
            }
         });
     }

}