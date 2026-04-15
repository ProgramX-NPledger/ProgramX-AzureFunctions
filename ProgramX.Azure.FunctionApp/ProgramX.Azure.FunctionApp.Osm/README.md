# Online Scout Manager (OSM) Integration

The architecture of the OSM integration is as shown below:

```
   Client Application -> Azure Function App -> OSM API
```

The Azure Function App is the client for OSM and handles authentication and communication with OSM.

Use the `IOsmClient` interface to integrate with OSM. The implementation of the `IOsmClient`
interface is provided by the `OsmClient` class. Calls to methods on the `OsmClient` class 
conduct their own authentication as required.

Use the REST API provided by the Azure Function App to perform OSM requests. The REST API will handle communications with OSM.

## Authentication

Authentication is performed in three phases.

### Configuration

An application must be defined in the OSM application, using the Settings > My Account Details > Developer Tools option.

1. Create an application using the **Create Application** button.
2. Enter a name for the application and click **Save**.
3. Confirm the application creation to obtain the required keys for OAuth2 authentication and click **Reveal Credentials**.
4. Store the keys in the configuration (probably appsettings.json):

| Key | appsettings.json key |
| --- | --- |
| OAuth Client ID | `Osm:ClientId` |
| OAuth Secret | `Osm:ClientSecret` |

### Initial Authentication

Initial authentication is performed as a once-only exercise. It involves initiating the OAuth2 key exchange.

Ensure that the environment variable `OsmOAuth2KeyCompletion` is set to the endpoint got key completion. The endpoint is at `/api/v1/scouts/osm/completekeyexchange` (you'll need to include the server address). Using environment variables in this way means that secrets are not stored in GitHub and different environments can be configured individually.

1. In order to ensure that tokens are collected locally, start the Azure Function App.
2. Perform a GET request to the URL [/api/v1/scouts/osm/initiatekeyexchange](/api/v1/scouts/osm/initiatekeyexchange)
3. This return a string with a URL that must be visited in a web browser.
4. Paste this URL into a web browser to initiate login. Authenticate with OSM as per usual. This calls a key completion endpoint (encoded in the outbound URL in the `redirect_uri` parameter) which retrieves the bearer and refresh tokens, storing them in the Azure Cosmos DB core/integration database/container.

**The refresh token may only be used once.**

### Ongoing Authentication

The `IOsmClient` interface should be used to perform all subsequent requests. The implementation uses an `HttpClient` to perform requests with the OSM API. This interaction will attach the Bearer token to the outgoing request, refreshing it as required using the `AuthTokenHandler` class. This will refresh the Bearer token using the cached Refresh token stored in step (5), above, updating with the next refresh token as required.


