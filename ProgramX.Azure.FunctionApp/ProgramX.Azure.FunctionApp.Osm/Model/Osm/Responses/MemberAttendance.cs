using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Osm.Model.Osm.Responses;

public class MemberAttendance
{
    [JsonPropertyName( "total")]
    public int Total { get; set; }

    [JsonPropertyName( "firstname")]
    public string FirstName { get; set; }

    [JsonPropertyName( "lastname")]
    public string LastName { get; set; }
    
    [JsonPropertyName( "photo_guid")]
    public Guid? PhotoGuid { get; set; }

    [JsonPropertyName( "patrolid")]
    public int PatrolId { get; set; }

    [JsonPropertyName( "patrolleader")]
    public string PatrolLeader { get; set; }

    [JsonPropertyName( "patrol")]
    public string Patrol { get; set; }

    [JsonPropertyName("dob")]
    public string DateOfBirth { get; set; }

    [JsonPropertyName("sectionid")]
    public int SectionId { get; set; }

    [JsonPropertyName("enddate")]
    public DateOnly? EndDate { get; set; }

    [JsonPropertyName("startdate")]
    public DateOnly StartDate { get; set; }
    
    [JsonPropertyName("age")]
    public string Age { get; set; }
    
    [JsonPropertyName("patrol_role_level_label")]
    public string PatrolRoleLevelLabel { get; set; }
    
    [JsonPropertyName("active")]
    public bool Active { get; set; }
    
    [JsonPropertyName("scoutid")]
    public int ScoutId { get; set; }
    
    [JsonExtensionData] 
    public Dictionary<string, JsonElement>? NativeAttendanceDateProperties { get; init; } // contains the dates in native OSM format

    [JsonIgnore]
    public IReadOnlyDictionary<DateOnly, bool> AttendanceDates => ParseAttendanceDates(NativeAttendanceDateProperties);

    private IReadOnlyDictionary<DateOnly, bool> ParseAttendanceDates(Dictionary<string, JsonElement>? data)
    {
        if (data is null || data.Count == 0)
            return new Dictionary<DateOnly, bool>();

        var result = new Dictionary<DateOnly, bool>();

        foreach (var (key, value) in data)
        {
            // Only treat keys like "2026-01-09" as attendance columns
            if (!DateOnly.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                continue;

            // Example input values are "Yes"/"No" (strings)
            if (value.ValueKind == JsonValueKind.String)
            {
                var s = value.GetString();

                // Adapt mapping rules to your API realities:
                if (string.Equals(s, "Yes", StringComparison.OrdinalIgnoreCase)) result[date] = true;
                else if (string.Equals(s, "No", StringComparison.OrdinalIgnoreCase)) result[date] = false;
                else if (string.Equals(s, "1", StringComparison.OrdinalIgnoreCase)) result[date] = true;
                else if (string.Equals(s, "0", StringComparison.OrdinalIgnoreCase)) result[date] = false;
                // else: unknown marker -> ignore or throw, your choice
            }
        
        }

        return result;
    }
}