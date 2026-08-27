namespace ProgramX.Azure.FunctionApp.Osm;

/// <summary>
/// Supplies OSM access tokens, hiding acquisition, caching and expiry from callers.
/// </summary>
/// <remarks>
/// Implementations are registered as singletons so that one token is shared by every
/// outbound OSM call, independent of <see cref="HttpMessageHandler"/> pool rotation.
/// </remarks>
public interface IOsmTokenProvider
{
    /// <summary>
    /// Returns a usable OSM access token, acquiring one if the cache is empty or expiring.
    /// </summary>
    /// <param name="forceRefresh">
    /// Discard the cached token and acquire a new one. Set this only after OSM has rejected
    /// the cached token with a 401 — concurrent callers still share a single acquisition.
    /// </param>
    /// <param name="cancellationToken">Cancels the token acquisition.</param>
    /// <exception cref="OsmException">The token endpoint refused to issue a token.</exception>
    Task<string> GetAccessTokenAsync(bool forceRefresh, CancellationToken cancellationToken);
}
