using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace ProgramX.Azure.FunctionApp.Osm;

/// <summary>
/// Attaches an OSM access token to every outbound request, and retries once if OSM rejects it.
/// </summary>
/// <remarks>
/// Token acquisition, caching and expiry belong to <see cref="IOsmTokenProvider"/>. Keeping them
/// out of this handler matters: handlers live in the <see cref="HttpMessageHandler"/> pool and are
/// rotated on a lifetime unrelated to token validity, and several can be alive at once, so any
/// state cached on the handler itself is both short-lived and not shared.
/// </remarks>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly IOsmTokenProvider _tokenProvider;
    private readonly ILogger<AuthTokenHandler> _logger;

    public AuthTokenHandler(IOsmTokenProvider tokenProvider, ILogger<AuthTokenHandler> logger)
    {
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Cloned up front because HttpRequestMessage is single-use: the first send consumes its
        // content stream, so the retry below needs its own copy. OSM requires form-encoded
        // bodies on POSTs, so this is not hypothetical.
        var retryCandidate = await CloneAsync(request, cancellationToken);

        var accessToken = await _tokenProvider.GetAccessTokenAsync(forceRefresh: false, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await base.SendAsync(request, cancellationToken);

        // Only 401 means "this token is not acceptable". A 403 means the token is valid but the
        // client lacks the scope for this endpoint — refreshing returns an identical token, so
        // retrying would just hide a configuration error as latency.
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            retryCandidate.Dispose();
            return response;
        }

        _logger.LogInformation("OSM returned 401 for {requestUri}; refreshing token and retrying once", request.RequestUri);

        string refreshedToken;
        try
        {
            refreshedToken = await _tokenProvider.GetAccessTokenAsync(forceRefresh: true, cancellationToken);
        }
        catch (OsmException osmException)
        {
            // Surfacing the original 401 would misreport a credentials problem as an auth failure
            // against the data endpoint, so let the token error bubble to the trigger instead.
            _logger.LogError(osmException, "Could not refresh OSM token after a 401");
            retryCandidate.Dispose();
            response.Dispose();
            throw;
        }

        retryCandidate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedToken);
        response.Dispose();

        return await base.SendAsync(retryCandidate, cancellationToken);
    }

    /// <summary>
    /// Copies a request so it can be sent a second time, buffering any content.
    /// </summary>
    private static async Task<HttpRequestMessage> CloneAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        if (request.Content is not null)
        {
            // Buffers the ORIGINAL content too, so reading it here does not consume a
            // non-seekable stream out from under the first send.
            await request.Content.LoadIntoBufferAsync();

            var buffered = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var clonedContent = new ByteArrayContent(buffered);
            foreach (var header in request.Content.Headers)
            {
                clonedContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = clonedContent;
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in ((IDictionary<string, object?>)request.Options))
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        return clone;
    }
}
