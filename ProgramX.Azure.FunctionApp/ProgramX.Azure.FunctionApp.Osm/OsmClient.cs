using System.Net.Http.Json;
using System.Transactions;
using Microsoft.Extensions.Configuration;
using ProgramX.Azure.FunctionApp.Contract;
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
    public async Task<IEnumerable<Term>> GetTerms(GetTermsCriteria criteria)
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

        return terms;
    }

    public async Task<IEnumerable<object>> GetMeetings(GetMeetingsCriteria criteria)
    {
       // https://www.onlinescoutmanager.co.uk/ext/programme/?action=getProgrammeSummary&sectionid=54338&termid=849238&verbose=1
       
        var uriBilder = new UriBuilder("https://www.onlinescoutmanager.co.uk/ext/programme/");
        uriBilder.Query = $"action=getProgrammeSummary&verbose=1&termid={criteria.TermId}"; // verbose=1 required to return primary_leader and badges
        uriBilder.Query += $"&sectionid={criteria.SectionId ?? SectionId}";
        
        var s = await _httpClient.GetStringAsync(uriBilder.Uri);
        var getMeetingsResponse = await _httpClient.GetFromJsonAsync<GetProgrammeSummaryResponse>(uriBilder.Uri);

        var meetings = TransformOsmProgrammeSummaryResponseItemsToMeetings(getMeetingsResponse.Items).ToList();
        var queryable = BuildQueryableForMeetingsCriteria(criteria,meetings);
        meetings = queryable.ToList();

        switch (criteria.SortBy)
        {
            case GetMeetingsSortBy.MeetingDate:
                meetings = meetings.OrderBy(q => q.Date).ToList();
                break;
        }

        return meetings;
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

    private IQueryable<Meeting> BuildQueryableForMeetingsCriteria(GetMeetingsCriteria criteria, IList<Meeting> meetings)
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
        if (criteria.OccursOnorAfter.HasValue) queryable = queryable.Where(q => q.Date>=criteria.OccursOnorAfter.Value);
        if (criteria.OccursOnOrBefore.HasValue) queryable = queryable.Where(q => q.Date<=criteria.OccursOnOrBefore.Value);
        return queryable;
    }

    public async Task<IEnumerable<Member>> GetMembers(GetMembersCriteria criteria)
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


    public async Task<IEnumerable<object>> GetFlexiRecords()
    {
        throw new NotImplementedException();
    }
}