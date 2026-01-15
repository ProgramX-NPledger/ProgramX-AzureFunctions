using System.Net.Http.Json;
using System.Transactions;
using Microsoft.Extensions.Configuration;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model.Osm;
using ProgramX.Azure.FunctionApp.Osm.Helpers;
using ProgramX.Azure.FunctionApp.Osm.Model;
using ProgramX.Azure.FunctionApp.Osm.Model.Criteria;

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

    public async Task<IEnumerable<object>> GetMeetings()
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Member>> GetMembers(GetMembersCriteria criteria)
    {
        // GET https://www.onlinescoutmanager.co.uk/ext/members/contact/?action=getListOfMembers&sort=dob&sectionid=54338&termid=849238&section=scouts
        
        var uriBilder = new UriBuilder("https://www.onlinescoutmanager.co.uk/ext/members/contact/");
        uriBilder.Query = $"action=getListOfMembers&sort={Translation.TranslateSortBy(criteria.SortBy)}&sectionid={SectionId}&termid={criteria.TermId}&section={criteria.SectionName}";
        var getMembersResponse = await _httpClient.GetFromJsonAsync<GetMembersResponse>(uriBilder.Uri);
        return getMembersResponse.Items.Select(q => new Member()
        {
            Age = Translation.TranslateAgeFromStringToPreciseAge(q.Age),
            FirstName = q.FirstName,
            LastName = q.LastName,
            FullName = q.FullName,
            IsActive = q.Active,
            OsmScoutId = q.ScoutId,
            PatrolRoleLevel = q.PatrolRoleLevelLabel,
        });
    }


    public async Task<IEnumerable<object>> GetFlexiRecords()
    {
        throw new NotImplementedException();
    }
}