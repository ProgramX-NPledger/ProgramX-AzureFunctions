using ProgramX.Azure.FunctionApp.Model.Osm;
using ProgramX.Azure.FunctionApp.Osm.Model;
using ProgramX.Azure.FunctionApp.Osm.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.Osm;

public interface IOsmClient
{
    /// <summary>
    /// Returns Terms within OSM.
    /// </summary>
    /// <param name="criteria">The criteria to apply to the request.</param>
    /// <returns>A collection of <see cref="Term"/> items.</returns>
    Task<IEnumerable<Term>> GetTermsAsync(GetTermsCriteria criteria);

    Task<IEnumerable<Meeting>> GetMeetingsAsync(GetMeetingsCriteria criteria);
    Task<IEnumerable<Member>> GetMembersAsync(GetMembersCriteria criteria);

    Task<IEnumerable<Attendance>> GetAttendanceAsync(GetAttendanceCriteria criteria);
    
    Task<IEnumerable<object>> GetFlexiRecords();

    // Task CreateFlexiRecord(int sectionId, string name, bool includeDateOfBirth, FlexiRecordInclude flexiRecordInclude,
    //     FlexiRecordNumbers flexiRecordNumbers);

} 