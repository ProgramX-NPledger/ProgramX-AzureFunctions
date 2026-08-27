using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Model.DTOs.Osm;
using ProgramX.Azure.FunctionApp.Model.DTOs.Osm.Response;
using ProgramX.Azure.FunctionApp.Model.Responses;
using ProgramX.Azure.FunctionApp.Osm;
using ProgramX.Azure.FunctionApp.Osm.Helpers;
using ProgramX.Azure.FunctionApp.Osm.Model;
using ProgramX.Azure.FunctionApp.Osm.Model.Criteria;
using GetMembersResponse = ProgramX.Azure.FunctionApp.Model.DTOs.Osm.Response.GetMembersResponse;

namespace ProgramX.Azure.FunctionApp.HttpTriggers.Scouting;

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

    // The InitiateKeyExchange / CompleteKeyExchange endpoints were removed when OSM auth moved to
    // the client_credentials grant. They implemented the one-time authorization-code exchange that
    // seeded a bearer + refresh token pair, which no longer exists: tokens are now acquired on
    // demand by IOsmTokenProvider from the client id and secret alone.
    //
    // Both were also publicly reachable — AuthorizationLevel.Anonymous with no RequiresAuthentication
    // wrapper — and CompleteKeyExchange returned an HTML page that postMessage'd the raw token JSON
    // to window.opener. Do not reinstate them without an authorisation wrapper.

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
             var attendances = await GetAttendancesBetweenDatesAsync(onOrAfter, onOrBefore, sectionId);

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