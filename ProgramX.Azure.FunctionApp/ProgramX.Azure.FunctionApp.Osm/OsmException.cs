using ProgramX.Azure.FunctionApp.Osm.Model.Osm.Responses;

namespace ProgramX.Azure.FunctionApp.Osm;

public sealed class OsmException : ApplicationException
{
    public OsmException(string message, string uri)
    : base(message)
    {
        
    }
    
    public OsmException(OsmTokenRefreshResponse osmTokenRefreshResponse)
        : base($"OSM token refresh error: {osmTokenRefreshResponse.Error}")
    {
        this.Data.Add("ErrorDescription", osmTokenRefreshResponse.ErrorDescription);
        this.Data.Add("Error", osmTokenRefreshResponse.Error);
        this.Data.Add("Hint", osmTokenRefreshResponse.Hint);
        this.Data.Add("Message", osmTokenRefreshResponse.Message);
    }
}