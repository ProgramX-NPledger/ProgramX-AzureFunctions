using ProgramX.Azure.FunctionApp.Osm.Model.Osm.Responses;

namespace ProgramX.Azure.FunctionApp.Osm;

public sealed class OsmException : ApplicationException
{
    /// <summary>
    /// The OSM URI the failed call was made against, where known.
    /// </summary>
    public string? Uri { get; }

    public OsmException(string message, string uri)
        : base($"{message} (uri: {uri})")
    {
        Uri = uri;
        this.Data.Add(nameof(Uri), uri);
    }

    public OsmException(OsmTokenRefreshResponse osmTokenRefreshResponse)
        : base($"OSM token error: {osmTokenRefreshResponse.Error} {osmTokenRefreshResponse.ErrorDescription}".TrimEnd())
    {
        this.Data.Add("ErrorDescription", osmTokenRefreshResponse.ErrorDescription);
        this.Data.Add("Error", osmTokenRefreshResponse.Error);
        this.Data.Add("Hint", osmTokenRefreshResponse.Hint);
        this.Data.Add("Message", osmTokenRefreshResponse.Message);
    }
}
