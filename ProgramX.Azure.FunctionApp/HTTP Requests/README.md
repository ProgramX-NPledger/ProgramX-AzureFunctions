# HTTP Requests
A series of HTTP requests that can be used to interact with the Program.X application.

## Applications

## HealthCheck

## Login

## Osm

Integration with the Online Scout Manager API. This integration requires authentication with the OSM API.

### How to authenticate with the OSM API

In order to authentication with the OSM API, an OAuth2 token must be obtained.

1. Use the endpoint `GET  {{host}}/api/v1/scouts/osm/initiatekeyexchange` in the `Osm.http` file. 
2. Browse to the URL in the `url` property returned, ensuring the `redirect_uri` matches the application URL defined in the OSM application _precisely_, ie. remove any encoding and verify the scheme is correct.
3. You will be redirected to the OSM login page. Log in with your OSM credentials.
4. This returns a JSON object containing the `access_token` and `refresh_token`. Both these must be set in the `appsettings.Development.json` file in the `Osm:BearerToken` and `Osm:RefreshToken` properties.
5. You can now use any other methods in the `Osm.http` file. Use of the Bearer token and refreshing of it will be automatic, using the `AuthTokenHandler` class.

### Getting Terms

Terms are retrieved using the `GET  {{host}}/api/v1/scouts/osm/terms` endpoint. A term represents a portion of an academic year.

The following parameters may be used:

| Parameter   | Required?  | Description                                                                                                       |
|-------------|------------|-------------------------------------------------------------------------------------------------------------------|
| `sectionId` | No         | The identifier of the section to return members for. If not specified, all members for the term will be returned. |


### Getting Members

Members are retrieved using the `GET  {{host}}/api/v1/scouts/osm/members` endpoint.

The following parameters may be used:

| Parameter   | Required?  | Description                                                                                                       |
|-------------|------------|-------------------------------------------------------------------------------------------------------------------|
| `termId`    | Yes        | The identifier of the term to return members for.                                                                 |
| `sectionId` | No         | The identifier of the section to return members for. If not specified, all members for the term will be returned. |



## Reset

## Roles

## Users
