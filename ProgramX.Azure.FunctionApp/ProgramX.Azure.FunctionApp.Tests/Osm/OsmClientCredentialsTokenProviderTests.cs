using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgramX.Azure.FunctionApp.Osm;
using ProgramX.Azure.FunctionApp.Tests.Mocks;

namespace ProgramX.Azure.FunctionApp.Tests.Osm;

[Category("Unit")]
[Category("Osm")]
[Category("OsmTokenProvider")]
[TestFixture]
public class OsmClientCredentialsTokenProviderTests
{
    private static OsmOptions ValidOptions() => new()
    {
        ClientId = "test-client-id",
        ClientSecret = "test-client-secret",
        Scopes = "section:member:read section:programme:read",
        SectionId = 54338,
        TokenExpirySkewSeconds = 60,
        FallbackTokenLifetimeSeconds = 3600
    };

    private static OsmClientCredentialsTokenProvider CreateProvider(
        StubHttpMessageHandler handler,
        OsmOptions? options = null)
        => new(
            new StubHttpClientFactory(handler),
            Options.Create(options ?? ValidOptions()),
            NullLogger<OsmClientCredentialsTokenProvider>.Instance);

    private static string TokenBody(string accessToken, int? expiresIn = 3600)
    {
        var expires = expiresIn.HasValue ? $"\"expires_in\":{expiresIn.Value}," : string.Empty;
        return $"{{\"token_type\":\"Bearer\",{expires}\"access_token\":\"{accessToken}\"}}";
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenTokenEndpointSucceeds_ReturnsAccessToken()
    {
        var handler = StubHttpMessageHandler.AlwaysReturns(HttpStatusCode.OK, TokenBody("token-1"));
        var provider = CreateProvider(handler);

        var token = await provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None);

        token.Should().Be("token-1");
    }

    [Test]
    public async Task GetAccessTokenAsync_UsesClientCredentialsGrantWithConfiguredScopes()
    {
        var handler = StubHttpMessageHandler.AlwaysReturns(HttpStatusCode.OK, TokenBody("token-1"));
        var provider = CreateProvider(handler);

        await provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None);

        var body = handler.Requests.Single().Body;
        body.Should().Contain("grant_type=client_credentials");
        body.Should().Contain("client_id=test-client-id");
        body.Should().Contain("client_secret=test-client-secret");
        // Scopes are space-separated and must survive form encoding.
        body.Should().Contain("section%3Amember%3Aread+section%3Aprogramme%3Aread");
        // There is no refresh token under this grant, so none should ever be sent.
        body.Should().NotContain("refresh_token");
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenCalledRepeatedly_AcquiresTokenOnlyOnce()
    {
        var handler = StubHttpMessageHandler.AlwaysReturns(HttpStatusCode.OK, TokenBody("token-1"));
        var provider = CreateProvider(handler);

        for (var i = 0; i < 5; i++)
        {
            await provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None);
        }

        handler.CallCount.Should().Be(1, "a cached, unexpired token must be reused");
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenManyCallersRaceOnAColdCache_AcquiresTokenOnlyOnce()
    {
        var handler = StubHttpMessageHandler.AlwaysReturns(HttpStatusCode.OK, TokenBody("token-1"));
        handler.ResponseDelay = TimeSpan.FromMilliseconds(50);
        var provider = CreateProvider(handler);

        var tokens = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None)));

        handler.CallCount.Should().Be(1, "refresh must be single-flight; OSM bans clients that call it excessively");
        tokens.Should().AllBe("token-1");
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenManyCallersForceRefreshTogether_AcquiresOnlyOneReplacement()
    {
        // This is the regression that motivated the redesign: under the old refresh-token model,
        // concurrent 401s each performed their own refresh, and because OSM refresh tokens are
        // single-use every loser burned the stored token and could brick the integration.
        var handler = StubHttpMessageHandler.ReturnsInSequence(
            (HttpStatusCode.OK, TokenBody("token-1")),
            (HttpStatusCode.OK, TokenBody("token-2")));
        handler.ResponseDelay = TimeSpan.FromMilliseconds(50);
        var provider = CreateProvider(handler);

        await provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None);
        handler.CallCount.Should().Be(1);

        var refreshed = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => provider.GetAccessTokenAsync(forceRefresh: true, CancellationToken.None)));

        handler.CallCount.Should().Be(2, "20 concurrent forced refreshes must collapse into one token request");
        refreshed.Should().AllBe("token-2");
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenTokenIsWithinExpirySkew_AcquiresANewToken()
    {
        var options = ValidOptions();
        // Skew exceeds the token lifetime, so the token is already within its expiry window.
        // The floor is dropped to zero so the assertion does not have to wait it out.
        options.TokenExpirySkewSeconds = 600;
        options.MinimumTokenLifetimeSeconds = 0;

        var handler = StubHttpMessageHandler.ReturnsInSequence(
            (HttpStatusCode.OK, TokenBody("token-1", expiresIn: 30)),
            (HttpStatusCode.OK, TokenBody("token-2", expiresIn: 30)));
        var provider = CreateProvider(handler, options);

        var first = await provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None);
        first.Should().Be("token-1");

        var second = await provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None);

        second.Should().Be("token-2", "an expired token must be replaced rather than reused");
        handler.CallCount.Should().Be(2);
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenExpiresInIsOmitted_StillReturnsAndCachesToken()
    {
        var handler = StubHttpMessageHandler.AlwaysReturns(
            HttpStatusCode.OK, TokenBody("token-1", expiresIn: null));
        var provider = CreateProvider(handler);

        var first = await provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None);
        var second = await provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None);

        first.Should().Be("token-1");
        second.Should().Be("token-1");
        handler.CallCount.Should().Be(1, "the configured fallback lifetime should apply");
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenTokenEndpointReturnsErrorBody_ThrowsOsmException()
    {
        var handler = StubHttpMessageHandler.AlwaysReturns(
            HttpStatusCode.BadRequest,
            "{\"error\":\"invalid_client\",\"error_description\":\"Client authentication failed\"}");
        var provider = CreateProvider(handler);

        var act = async () => await provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<OsmException>();
        exception.Which.Message.Should().Contain("invalid_client");
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenTokenEndpointReturnsSuccessWithoutAccessToken_ThrowsOsmException()
    {
        var handler = StubHttpMessageHandler.AlwaysReturns(
            HttpStatusCode.OK, "{\"token_type\":\"Bearer\",\"expires_in\":3600}");
        var provider = CreateProvider(handler);

        var act = async () => await provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None);

        await act.Should().ThrowAsync<OsmException>();
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenTokenEndpointReturnsNonJson_ThrowsOsmException()
    {
        // OSM is documented to return non-JSON from some endpoints, so this must not surface as
        // a raw JsonException.
        var handler = StubHttpMessageHandler.AlwaysReturns(
            HttpStatusCode.ServiceUnavailable, "<html><body>Service Unavailable</body></html>");
        var provider = CreateProvider(handler);

        var act = async () => await provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None);

        await act.Should().ThrowAsync<OsmException>();
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenAnEarlierAcquisitionFailed_RetriesOnTheNextCall()
    {
        var handler = StubHttpMessageHandler.ReturnsInSequence(
            (HttpStatusCode.ServiceUnavailable, "{\"error\":\"temporarily_unavailable\"}"),
            (HttpStatusCode.OK, TokenBody("token-1")));
        var provider = CreateProvider(handler);

        var failing = async () => await provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None);
        await failing.Should().ThrowAsync<OsmException>();

        var token = await provider.GetAccessTokenAsync(forceRefresh: false, CancellationToken.None);

        token.Should().Be("token-1", "a failed acquisition must not poison the provider");
    }
}
