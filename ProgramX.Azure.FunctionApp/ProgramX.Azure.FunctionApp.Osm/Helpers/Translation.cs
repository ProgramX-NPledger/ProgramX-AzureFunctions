using ProgramX.Azure.FunctionApp.Model;
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

}