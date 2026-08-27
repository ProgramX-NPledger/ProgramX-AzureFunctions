using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProgramX.Azure.FunctionApp.Osm.Model.Osm.Responses;

namespace ProgramX.Azure.FunctionApp.Osm;

/// <summary>
/// Acquires OSM access tokens with the OAuth 2.0 <c>client_credentials</c> grant.
/// </summary>
/// <remarks>
/// <para>
/// Under this grant an access token is a pure function of (client id, client secret, scopes),
/// so there is no refresh token and nothing durable to lose. Losing the cached token costs one
/// extra token request, not an outage, which is why the cache is in-memory only and no
/// cross-instance coordination is needed — N Function instances holding N tokens is fine.
/// </para>
/// <para>
/// Registered as a singleton. Refreshes are single-flight: concurrent callers that arrive
/// while an acquisition is in progress wait for it and reuse its result rather than issuing
/// their own token request. That matters because OSM bans clients that call it excessively.
/// </para>
/// </remarks>
public class OsmClientCredentialsTokenProvider : IOsmTokenProvider
{
    /// <summary>
    /// Name of the <see cref="IHttpClientFactory"/> client used for token requests. It must not
    /// have <see cref="AuthTokenHandler"/> attached, or acquiring a token would recurse.
    /// </summary>
    public const string TokenHttpClientName = "osm-token";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<OsmOptions> _options;
    private readonly ILogger<OsmClientCredentialsTokenProvider> _logger;

    private readonly SemaphoreSlim _acquisitionLock = new(1, 1);

    /// <summary>
    /// The cached token. Replaced wholesale rather than mutated, so readers always observe a
    /// self-consistent token/expiry pair without taking the lock.
    /// </summary>
    private volatile CachedToken? _cached;

    public OsmClientCredentialsTokenProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<OsmOptions> options,
        ILogger<OsmClientCredentialsTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetAccessTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh)
        {
            var cached = _cached;
            if (cached is not null && cached.IsUsableAt(DateTimeOffset.UtcNow)) return cached.AccessToken;
        }

        // Captured before taking the lock so that, after waiting, we can tell whether another
        // caller replaced the token while we queued. If it did, its token is ours to use and we
        // must not acquire again — otherwise every concurrent 401 becomes its own token request.
        var staleWhenQueued = _cached;

        await _acquisitionLock.WaitAsync(cancellationToken);
        try
        {
            var current = _cached;
            var replacedWhileWaiting = !ReferenceEquals(current, staleWhenQueued);

            if (current is not null
                && current.IsUsableAt(DateTimeOffset.UtcNow)
                && (replacedWhileWaiting || !forceRefresh))
            {
                return current.AccessToken;
            }

            var fresh = await AcquireTokenAsync(cancellationToken);
            _cached = fresh;
            return fresh.AccessToken;
        }
        finally
        {
            _acquisitionLock.Release();
        }
    }

    private async Task<CachedToken> AcquireTokenAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
            ["scope"] = options.Scopes
        };

        var httpClient = _httpClientFactory.CreateClient(TokenHttpClientName);

        _logger.LogInformation("Acquiring OSM access token via client_credentials for scopes {scopes}", options.Scopes);

        using var response = await httpClient.PostAsync(
            options.TokenEndpointUri,
            new FormUrlEncodedContent(form),
            cancellationToken);

        // Read as string first: the body is the only diagnostic when OSM refuses, and OSM is
        // documented to sometimes return non-JSON.
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        OsmTokenRefreshResponse? token;
        try
        {
            token = JsonSerializer.Deserialize<OsmTokenRefreshResponse>(body);
        }
        catch (JsonException jsonException)
        {
            _logger.LogError(jsonException,
                "OSM token endpoint returned unparseable content with status {statusCode}", response.StatusCode);
            throw new OsmException(
                $"OSM token endpoint returned unparseable content with status {(int)response.StatusCode}.",
                options.TokenEndpoint);
        }

        if (token is null)
        {
            throw new OsmException(
                $"OSM token endpoint returned an empty body with status {(int)response.StatusCode}.",
                options.TokenEndpoint);
        }

        // OSM signals failure both by status code and by an error body, so check both. A 400
        // invalid_client here means the client id/secret are wrong or the client is not
        // permitted to use client_credentials.
        if (!response.IsSuccessStatusCode || token.IsError)
        {
            _logger.LogError(
                "OSM token request failed with status {statusCode}: {error} {errorDescription} {hint}",
                response.StatusCode, token.Error, token.ErrorDescription, token.Hint);
            throw new OsmException(token);
        }

        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new OsmException(
                "OSM token endpoint reported success but returned no access_token.",
                options.TokenEndpoint);
        }

        var lifetimeSeconds = token.ExpiresIn ?? options.FallbackTokenLifetimeSeconds;
        if (token.ExpiresIn is null)
        {
            _logger.LogWarning(
                "OSM token response omitted expires_in; assuming {fallbackSeconds}s",
                options.FallbackTokenLifetimeSeconds);
        }

        // Expire early by the skew so a token is never handed out with so little life left that
        // it dies in flight. Floored so a pathologically short lifetime cannot cause a refresh
        // on every single call.
        var usableUntil = DateTimeOffset.UtcNow
            .AddSeconds(lifetimeSeconds)
            .AddSeconds(-options.TokenExpirySkewSeconds);
        var floor = DateTimeOffset.UtcNow.AddSeconds(options.MinimumTokenLifetimeSeconds);
        if (usableUntil < floor) usableUntil = floor;

        _logger.LogInformation("Acquired OSM access token, usable until {usableUntil:o}", usableUntil);

        return new CachedToken(token.AccessToken, usableUntil);
    }

    private sealed record CachedToken(string AccessToken, DateTimeOffset UsableUntil)
    {
        public bool IsUsableAt(DateTimeOffset instant) => instant < UsableUntil;
    }
}
