using Microsoft.Extensions.Options;

namespace ProgramX.Azure.FunctionApp.Osm;

/// <summary>
/// Configuration for the OSM integration, bound from the <c>Osm</c> configuration section.
/// </summary>
/// <remarks>
/// Existing configuration files declare these as flat colon-delimited keys at the document
/// root (<c>"Osm:ClientId"</c> rather than a nested <c>"Osm": { "ClientId": … }</c> object).
/// Both shapes bind identically, so no configuration file needs restructuring.
/// </remarks>
public class OsmOptions
{
    public const string SectionName = "Osm";

    /// <summary>
    /// OAuth client id issued by OSM (Settings > My Account Details > Developer Tools).
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth client secret issued by OSM. Under the client_credentials grant this is the only
    /// long-lived secret the integration holds, so it is the only thing that needs rotating.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Space-separated OAuth scopes, e.g. <c>section:member:read section:programme:read</c>.
    /// </summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// The OSM section the integration reads by default when a request does not specify one.
    /// </summary>
    public int SectionId { get; set; }

    /// <summary>
    /// Root address for OSM API calls.
    /// </summary>
    public string BaseAddress { get; set; } = "https://www.onlinescoutmanager.co.uk/";

    /// <summary>
    /// OAuth token endpoint.
    /// </summary>
    public string TokenEndpoint { get; set; } = "https://www.onlinescoutmanager.co.uk/oauth/token";

    /// <summary>
    /// How far ahead of the real expiry a token is treated as expired, so a request is never
    /// issued with a token that dies mid-flight.
    /// </summary>
    public int TokenExpirySkewSeconds { get; set; } = 60;

    /// <summary>
    /// Assumed token lifetime when OSM's token response omits <c>expires_in</c>.
    /// </summary>
    public int FallbackTokenLifetimeSeconds { get; set; } = 3600;

    /// <summary>
    /// Floor on how long a freshly acquired token is treated as usable, so that a very short
    /// lifetime (or a skew larger than the lifetime) cannot cause a token request per API call.
    /// </summary>
    public int MinimumTokenLifetimeSeconds { get; set; } = 30;

    /// <summary>
    /// <see cref="BaseAddress"/> as a <see cref="Uri"/>. Only valid once options have passed
    /// <see cref="OsmOptionsValidator"/>.
    /// </summary>
    public Uri BaseAddressUri => new(BaseAddress);

    /// <summary>
    /// <see cref="TokenEndpoint"/> as a <see cref="Uri"/>. Only valid once options have passed
    /// <see cref="OsmOptionsValidator"/>.
    /// </summary>
    public Uri TokenEndpointUri => new(TokenEndpoint);
}

/// <summary>
/// Fails startup when the OSM configuration is unusable, rather than at the first OSM call.
/// </summary>
public class OsmOptionsValidator : IValidateOptions<OsmOptions>
{
    public ValidateOptionsResult Validate(string? name, OsmOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ClientId))
            failures.Add($"{OsmOptions.SectionName}:{nameof(OsmOptions.ClientId)} is required.");

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            failures.Add($"{OsmOptions.SectionName}:{nameof(OsmOptions.ClientSecret)} is required.");

        if (string.IsNullOrWhiteSpace(options.Scopes))
            failures.Add($"{OsmOptions.SectionName}:{nameof(OsmOptions.Scopes)} is required "
                         + "(space-separated, e.g. 'section:member:read section:programme:read').");

        if (options.SectionId <= 0)
            failures.Add($"{OsmOptions.SectionName}:{nameof(OsmOptions.SectionId)} must be a positive OSM section id.");

        if (!Uri.TryCreate(options.BaseAddress, UriKind.Absolute, out _))
            failures.Add($"{OsmOptions.SectionName}:{nameof(OsmOptions.BaseAddress)} must be an absolute URI.");

        if (!Uri.TryCreate(options.TokenEndpoint, UriKind.Absolute, out _))
            failures.Add($"{OsmOptions.SectionName}:{nameof(OsmOptions.TokenEndpoint)} must be an absolute URI.");

        if (options.TokenExpirySkewSeconds < 0)
            failures.Add($"{OsmOptions.SectionName}:{nameof(OsmOptions.TokenExpirySkewSeconds)} cannot be negative.");

        if (options.FallbackTokenLifetimeSeconds <= 0)
            failures.Add($"{OsmOptions.SectionName}:{nameof(OsmOptions.FallbackTokenLifetimeSeconds)} must be positive.");

        if (options.MinimumTokenLifetimeSeconds < 0)
            failures.Add($"{OsmOptions.SectionName}:{nameof(OsmOptions.MinimumTokenLifetimeSeconds)} cannot be negative.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
