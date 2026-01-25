using System.Net.Http.Json;
using System.Transactions;
using Microsoft.Extensions.Configuration;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Osm;
using ProgramX.Azure.FunctionApp.Osm.Helpers;
using ProgramX.Azure.FunctionApp.Osm.Model;
using ProgramX.Azure.FunctionApp.Osm.Model.Criteria;
using ProgramX.Azure.FunctionApp.Osm.Model.Osm.Responses;

namespace ProgramX.Azure.FunctionApp.Osm;

public class OsmClient : IOsmClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public int SectionId { get; private set; } 
    
    public OsmClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        
        var sectionIdAsString = _configuration["Osm:SectionId"];
        if (!string.IsNullOrWhiteSpace(sectionIdAsString)) SectionId = int.Parse(sectionIdAsString);
            else SectionId = 54338;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Term>> GetTermsAsync(GetTermsCriteria criteria)
    {
        var uriBuilder = new UriBuilder("https://www.onlinescoutmanager.co.uk/api.php");
        uriBuilder.Query = $"action=getTerms";
        
        var sectionId = criteria.SectionId ?? SectionId;

        var result = await _httpClient.GetFromJsonAsync<Dictionary<string, List<GetTermsResponseTerm>>>(uriBuilder.Uri);

        var terms = new List<Term>();
        
        // response has the SectionID has the property containing the terms, so have to dynamically parse
        // all terms for all sections are returned so we use the criteria to pick out the section we want, if no section provided use the configured section ID
        if (result != null && result.TryGetValue(sectionId.ToString(), out var getTermsResponseTerms))
        {
            foreach (var getTermsResponseTerm in getTermsResponseTerms)
            {
                terms.Add(new Term()
                {
                    SectionId = criteria.SectionId ?? SectionId,
                    Name = getTermsResponseTerm.Name,
                    EndDate = getTermsResponseTerm.EndDate,
                    StartDate = getTermsResponseTerm.StartDate,
                    OsmTermId = getTermsResponseTerm.TermId,
                    MasterTerm = getTermsResponseTerm.MasterTerm,
                    IsPast = getTermsResponseTerm.Past,
                });
            }
        }
        
        var queryable = BuildQueryableForGetTermsCriteria(criteria,terms);
        terms = queryable.ToList();

        return terms;
    }

    public async Task<IEnumerable<Meeting>> GetMeetingsAsync(GetMeetingsCriteria criteria)
    {
       // https://www.onlinescoutmanager.co.uk/ext/programme/?action=getProgrammeSummary&sectionid=54338&termid=849238&verbose=1
       
        var uriBilder = new UriBuilder("https://www.onlinescoutmanager.co.uk/ext/programme/");
        uriBilder.Query = $"action=getProgrammeSummary&verbose=1&termid={criteria.TermId}"; // verbose=1 required to return primary_leader and badges
        uriBilder.Query += $"&sectionid={criteria.SectionId ?? SectionId}";
        
        var s = await _httpClient.GetStringAsync(uriBilder.Uri);
        var getMeetingsResponse = await _httpClient.GetFromJsonAsync<GetProgrammeSummaryResponse>(uriBilder.Uri);

        var meetings = TransformOsmProgrammeSummaryResponseItemsToMeetings(getMeetingsResponse.Items).ToList();
        var queryable = BuildQueryableForGetMeetingsCriteria(criteria,meetings);
        meetings = queryable.ToList();

        switch (criteria.SortBy)
        {
            case GetMeetingsSortBy.MeetingDate:
                meetings = meetings.OrderBy(q => q.Date).ToList();
                break;
        }

        return meetings;
    }
    
    public async Task<IEnumerable<Member>> GetMembersAsync(GetMembersCriteria criteria)
    {
        // GET https://www.onlinescoutmanager.co.uk/ext/members/contact/?action=getListOfMembers&sort=dob&sectionid=54338&termid=849238&section=scouts
        
        var uriBilder = new UriBuilder("https://www.onlinescoutmanager.co.uk/ext/members/contact/");
        uriBilder.Query = $"action=getListOfMembers&sort={Translation.TranslateSortBy(criteria.SortBy)}&termid={criteria.TermId}&section={criteria.SectionName}";
        uriBilder.Query += $"&sectionid={criteria.SectionId ?? SectionId}";
        
        var s = await _httpClient.GetStringAsync(uriBilder.Uri);
        var getMembersResponse = await _httpClient.GetFromJsonAsync<GetMembersResponse>(uriBilder.Uri);
        return getMembersResponse.Items.Select(q => new Member()
        {
            Age = Translation.TranslateAgeFromStringToPreciseAge(q.Age),
            FirstName = q.FirstName,
            LastName = q.LastName,
            FullName = q.FullName,
            IsActive = q.IsActive,
            OsmScoutId = q.OsmScoutId,
            PatrolRoleLevel = q.PatrolRoleLevelLabel,
        });
    }


    public async Task<IEnumerable<Attendance>> GetAttendanceAsync(GetAttendanceCriteria criteria)
    {
        // https://www.onlinescoutmanager.co.uk/ext/members/attendance/?action=get&sectionid=54338&termid=849238&section=scouts&nototal=true
        
        var uriBuilder = new UriBuilder("https://www.onlinescoutmanager.co.uk/ext/members/attendance/");
        uriBuilder.Query = $"action=get&termid={criteria.TermId}"; // verbose=1 required to return primary_leader and badges
        uriBuilder.Query += $"&sectionid={criteria.SectionId ?? SectionId}";
        uriBuilder.Query += "&nototal=true";
        uriBuilder.Query += "&section=scouts";
        
        var getAttendanceResponse = await _httpClient.GetFromJsonAsync<GetAttendanceResponse>(uriBuilder.Uri);

        var attendance = TransformOsmAttendanceResponseItemsToAttendances(getAttendanceResponse.Items).ToList();
        var queryable = BuildQueryableForGetAttendanceCriteria(criteria,attendance);
        attendance = queryable.ToList();
        attendance = FilteredWithinDateCriteria(criteria,attendance);
        
        switch (criteria.SortBy)
        {
            case GetAttendanceSortBy.LastName:
                attendance = attendance.OrderBy(q => q.LastName).ToList();
                break;
        }

        return attendance;
    }

    private List<Attendance> FilteredWithinDateCriteria(GetAttendanceCriteria criteria, List<Attendance> attendance)
    {
        foreach (var attendanceOverTerm in attendance)
        {
            foreach (var meetingDate in attendanceOverTerm.AttendanceOverTerm.Keys)
            {
                if (criteria.OnOrBefore.HasValue)
                {
                    if (meetingDate < criteria.OnOrBefore.Value)
                        attendanceOverTerm.AttendanceOverTerm.Remove(meetingDate);
                }

                if (criteria.OnOrAfter.HasValue)
                {
                    if (meetingDate > criteria.OnOrAfter.Value)
                        attendanceOverTerm.AttendanceOverTerm.Remove(meetingDate);
                }
            }
        }
        
        return attendance;
    }


    private IEnumerable<Meeting> TransformOsmProgrammeSummaryResponseItemsToMeetings(IEnumerable<Evening> osmEvenings)
    {
        return new List<Meeting>(
            osmEvenings.Select(q => new Meeting()
            {
                ParentsRequiredCount = q.ParentsRequiredCount,
                PrimaryLeader = TransformOsmPrimaryLeaderToMember(q.PrimaryLeader),
                Badges = (q.Badges ?? Array.Empty<Badge>()).Select(b => TransformOsmBadgeToBadge(b)),
                Date = q.MeetingDate,
                Title = q.Title,
                OsmEveningId = q.EveningId,
                ParentsOutstandingCount = q.ParentsRequiredCount - q.ParentsAttendingCount,
                UnavailableLeadersCount = q.UnavailableLeaders
            }));
    }

    private ConciseBadge TransformOsmBadgeToBadge(Badge badge)
    {
        return new ConciseBadge()
        {
            Name = badge.Name,
            OsmImagePath = badge.ImagePath
        };
    }

    private ConciseMember? TransformOsmPrimaryLeaderToMember(Leader? leader)
    {
        if (leader == null) return null;
        return new ConciseMember()
        {
            FirstName = leader.FirstName,
            LastName = leader.LastName,
            OsmPhotoGuid = leader.PhotoId,
            OsmScoutId = leader.MemberId,
        };
    }

    private IQueryable<Meeting> BuildQueryableForGetMeetingsCriteria(GetMeetingsCriteria criteria, IList<Meeting> meetings)
    {
        var queryable = meetings.AsQueryable();
        if (criteria.HasOutstandingRequiredParents.HasValue)
        {
            if (criteria.HasOutstandingRequiredParents.Value) queryable = queryable.Where(q => q.ParentsOutstandingCount>0);
            else queryable = queryable.Where(q => q.ParentsOutstandingCount==0);
        }
        if (criteria.HasPrimaryLeader.HasValue)
        {
            if (criteria.HasPrimaryLeader.Value) queryable = queryable.Where(q => q.PrimaryLeader!=null);
            else queryable = queryable.Where(q => q.PrimaryLeader==null);   
        }
        if (criteria.Keywords!=null && criteria.Keywords.Any()) queryable = queryable.Where(q => criteria.Keywords.Any(k => q.Title.ToLower().Contains(k.ToLower())));
        if (criteria.OccursOnOrAfter.HasValue) queryable = queryable.Where(q => q.Date>=criteria.OccursOnOrAfter.Value);
        if (criteria.OccursOnOrBefore.HasValue) queryable = queryable.Where(q => q.Date<=criteria.OccursOnOrBefore.Value);
        return queryable;
    }
    
    
    private IQueryable<Term> BuildQueryableForGetTermsCriteria(GetTermsCriteria criteria, IList<Term> terms)
    {
        var queryable = terms.AsQueryable();

        if (criteria.StartsOnOrAfter.HasValue) queryable = queryable.Where(q => q.StartDate>=criteria.StartsOnOrAfter.Value);
        if (criteria.EndsOnOrBefore.HasValue) queryable = queryable.Where(q => q.EndDate<=criteria.EndsOnOrBefore.Value);
        
        return queryable;
    }
    

    private IQueryable<Attendance> BuildQueryableForGetAttendanceCriteria(GetAttendanceCriteria criteria, List<Attendance> attendance)
    {
        var queryable = attendance.AsQueryable();
        
        if (criteria.MemberId.HasValue) queryable = queryable.Where(q => q.OsmScoutId == criteria.MemberId.Value);

        return queryable;
    }

    private IEnumerable<Attendance> TransformOsmAttendanceResponseItemsToAttendances(IEnumerable<MemberAttendance> osmAttendance)
    {
        return osmAttendance.Select(q => new Attendance()
        {
            FirstName = q.FirstName,
            LastName = q.LastName,
            PatrolName = q.Patrol,
            PatrolRoleLevelLabel = q.PatrolRoleLevelLabel,
            OsmPhotoGuid = q.PhotoGuid,
            Age = Translation.TranslateAgeFromStringToPreciseAge(q.Age),
            DateOfBirth = Translation.TranslateStringToNullableDateOnly(q.DateOfBirth),
            EndDate = q.EndDate,
            IsActive = q.Active,
            IsPatrolLeader = q.PatrolLeader=="1",
            OsmPatrolId = q.PatrolId,
            OsmScoutId = q.ScoutId,
            OsmSectionId = q.SectionId,
            StartDate = q.StartDate,
            AttendanceOverTerm = q.AttendanceDates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        });
    }

    public async Task<IEnumerable<object>> GetFlexiRecords()
    {
        throw new NotImplementedException();
    }
}