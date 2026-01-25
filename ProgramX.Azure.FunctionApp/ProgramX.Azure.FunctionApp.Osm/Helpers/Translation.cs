using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Osm.Model;
using ProgramX.Azure.FunctionApp.Osm.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.Osm.Helpers;

public static class Translation
{
    public static PreciseAge? TranslateAgeFromStringToPreciseAge(string osmAgeFormat)
    {
        if (string.IsNullOrWhiteSpace(osmAgeFormat))
            return null;
        
        // decode the string to remove encodings
        var decodedOsmAgeFormat = osmAgeFormat.Replace("\\", string.Empty);

        var yearsAsString = decodedOsmAgeFormat.Substring(0, decodedOsmAgeFormat.IndexOf("/", StringComparison.Ordinal)).Trim();
        var monthsAsString = decodedOsmAgeFormat.Substring(decodedOsmAgeFormat.IndexOf("/", StringComparison.Ordinal) + 1).Trim();
        return new PreciseAge(byte.Parse(yearsAsString), byte.Parse(monthsAsString));
    }
    

    public static string TranslateSortBy(GetMembersSortBy criteriaSortBy)
    {
        switch (criteriaSortBy)
        {
            case GetMembersSortBy.DateOfBirth:
                return "dob";
        }
        throw new NotSupportedException($"{criteriaSortBy} is not supported.");
    }

    public static DateOnly? TranslateStringToNullableDateOnly(string s)
    {
        if (string.IsNullOrWhiteSpace(s) || !DateOnly.TryParse(s, out var dateOnly))
            return null;
        
        return dateOnly;
    }

    public static DateRange TranslateStringsToDateRange(string? onOrAfter, string? onOrBefore)
    {
        var dateRange = new DateRange();
        if (!string.IsNullOrWhiteSpace(onOrAfter))
        {
            DateOnly parsedDateOnly;
            if (DateOnly.TryParse(onOrAfter,out parsedDateOnly)) dateRange.OnOrAfter = parsedDateOnly;
        }
        if (!string.IsNullOrWhiteSpace(onOrBefore))
        {
            DateOnly parsedDateOnly;
            if (DateOnly.TryParse(onOrBefore,out parsedDateOnly)) dateRange.OnOrBefore = parsedDateOnly;
        }
        return dateRange;
    }

}