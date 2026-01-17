using ProgramX.Azure.FunctionApp.Model.Osm;
using ProgramX.Azure.FunctionApp.Osm.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.Osm;

public interface IOsmClient
{
    /// <summary>
    /// Returns Terms within OSM.
    /// </summary>
    /// <param name="criteria">The criteria to apply to the request.</param>
    /// <returns>A collection of <see cref="Term"/> items.</returns>
    Task<IEnumerable<Term>> GetTerms(GetTermsCriteria criteria);
    Task<IEnumerable<object>> GetMeetings();
    Task<IEnumerable<Member>> GetMembers(GetMembersCriteria criteria);
    Task<IEnumerable<object>> GetFlexiRecords();
     
}