# Online Scout Manager (OSM) Integration

The architecture of the OSM integration is as shown below:

```
   Client Application -> Azure Function App -> OSM API
```

The Azure Function App is the client for OSM and handles authentication and communication with OSM.

Use the `IOsmClient` interface to integrate with OSM. The implementation of the `IOsmClient`
interface is provided by the `OsmClient` class. Authentication is applied transparently by an
`HttpClient` message handler, so callers never deal with tokens.

Use the REST API provided by the Azure Function App to perform OSM requests. The REST API will handle communications with OSM.

## Authentication

OSM is authenticated with the OAuth 2.0 **`client_credentials`** grant. There is no browser-based
key exchange, no refresh token, and nothing persisted: an access token is a pure function of
(client id, client secret, scopes), so it can always be re-obtained on demand.

### Configuration

An application must be defined in the OSM application, using the Settings > My Account Details > Developer Tools option.

1. Create an application using the **Create Application** button.
2. Enter a name for the application and click **Save**.
3. Confirm the application creation to obtain the required keys for OAuth2 authentication and click **Reveal Credentials**.
4. Store the keys in configuration. All keys bind to `OsmOptions` from the `Osm` section:

| OSM value | Configuration key | Required |
| --- | --- | --- |
| OAuth Client ID | `Osm:ClientId` | yes |
| OAuth Secret | `Osm:ClientSecret` | yes |
| Requested scopes, space separated | `Osm:Scopes` | yes |
| Default OSM section id | `Osm:SectionId` | yes |
| API root | `Osm:BaseAddress` | no, defaults to `https://www.onlinescoutmanager.co.uk/` |
| Token endpoint | `Osm:TokenEndpoint` | no, defaults to `…/oauth/token` |
| Early-expiry margin, seconds | `Osm:TokenExpirySkewSeconds` | no, defaults to 60 |
| Assumed lifetime when `expires_in` is absent | `Osm:FallbackTokenLifetimeSeconds` | no, defaults to 3600 |
| Floor on cached token lifetime, seconds | `Osm:MinimumTokenLifetimeSeconds` | no, defaults to 30 |

The client secret is the only long-lived secret the integration holds, so rotating OSM credentials
is a configuration change with no re-authorisation step. Never commit it: locally use
`dotnet user-secrets` or the encrypted `local.settings.json`; in Azure use a Key Vault reference
resolved by the Function App's managed identity.

### Runtime

`IOsmTokenProvider` (implemented by `OsmClientCredentialsTokenProvider`, registered as a
**singleton**) owns the token:

- It caches the access token in memory with its expiry, and refreshes proactively once the token
  is within `TokenExpirySkewSeconds` of expiring — so a request is never issued with a token that
  dies in flight.
- Refreshes are **single-flight**: concurrent callers share one token request rather than each
  issuing their own. OSM bans clients that call it excessively, so this matters.
- The cache is per-instance and deliberately not distributed. Several Function instances each
  holding their own token is correct under this grant; losing a cached token costs one extra token
  request, not an outage.

`AuthTokenHandler` attaches the token to each outbound request. If OSM answers **401** it forces
one refresh and retries the request once, on a clone (an `HttpRequestMessage` cannot be sent
twice). A **403** is *not* retried: under scoped `client_credentials` it means the client lacks the
scope for that endpoint, so a refresh would return an identical token and mask a configuration
error. If you get a 403, widen `Osm:Scopes` and re-check the credentials in OSM.

### Previously

Earlier versions used the `authorization_code` grant: an operator visited an OSM authorise URL,
and `scouts/osm/initiatekeyexchange` / `scouts/osm/completekeyexchange` seeded a bearer and refresh
token pair into Cosmos `core`/`integrations`, thereafter self-renewing. Those endpoints have been
removed. They were publicly reachable, and because OSM refresh tokens are single-use, concurrent
refreshes could burn the stored token and leave the integration unusable until the exchange was
re-run by hand. `client_credentials` has neither problem.

`IIntegrationRepository` and the `integrations` container remain in the codebase but are no longer
used by the OSM integration.
