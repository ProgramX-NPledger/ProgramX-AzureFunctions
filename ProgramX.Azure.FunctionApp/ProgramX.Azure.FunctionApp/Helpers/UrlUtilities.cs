namespace ProgramX.Azure.FunctionApp.Helpers;

public class UrlUtilities
{
    /// <summary>
    /// Returns the integer value of the query string parameter, or null if the parameter is not present or is not a valid integer.
    /// </summary>
    /// <param name="s">Query string parameter.</param>
    /// <returns>A valid integer or <c>nukk</c> if not valid or specified.</returns>
    public static int? GetValidIntegerQueryStringParameterOrNull(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (!int.TryParse(s, out var offset)) return null;
        if (offset < 0) return null;
        return offset;
    }

}