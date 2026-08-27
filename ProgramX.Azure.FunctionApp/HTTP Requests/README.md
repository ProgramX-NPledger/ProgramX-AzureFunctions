# HTTP Requests
A series of HTTP requests that can be used to interact with the Program.X application.

## Applications

## HealthCheck

## Login

## Osm

Integration with the Online Scout Manager API. This integration requires authentication with the OSM API.

### How to authenticate with the OSM API

There is nothing to do. OSM uses the OAuth2 `client_credentials` grant, so the Function App obtains
and renews its own access tokens from `Osm:ClientId` and `Osm:ClientSecret`. Set those two keys (plus
`Osm:Scopes` and `Osm:SectionId`) and every request in `Osm.http` will work.

The former manual key exchange — `initiatekeyexchange`, browsing to an OSM authorise URL, then
pasting `access_token` / `refresh_token` into `appsettings.Development.json` — is gone, along with
the `Osm:BearerToken` and `Osm:RefreshToken` keys. See
`ProgramX.Azure.FunctionApp.Osm/README.md` for the current model.

Note that `{{token}}` in these requests is the **Program.X** JWT from
`POST {{host}}/api/v1/login`, not an OSM token. Put it in `http-client.private.env.json`, which is
gitignored — not in the committed `http-client.env.json`.

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

### Getting Meetings

Terms are retrieved using the `GET  {{host}}/api/v1/scouts/osm/meetings` endpoint. A meeting represents a repeatable meeting that is not exceptional, like an Event.

The following parameters may be used:

| Parameter   | Required?  | Description                                                                                                                                                                 |
|-------------|------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `termId`    | Yes        | The identifier of the term to return members for.                                                                                                                           |
| `sectionId` | No         | The identifier of the section to return members for. If not specified, all members for the term will be returned.                                                           |
| `hasOutstandingRequiredParents` | No         | Set to `true` to only return meetings that have outstanding required parents. Set to `false` to return all meetings that do not have outstanding required parents.          |
| `hasPrimaryLeader` | No         | Set to `true` to only return meetings that have a primary leader. Set to `false` to return all meetings that do not have a primary leader.                                   |
| `keywords` | No         | A comma separated list of keywords to filter meetings by. Filtering is performed on the Title of the meeting. |
| `onOrAfter` | No         | Return meetings that start on or after the specified date. |
| `onOrBefore` | No         | Return meetings that start on or before the specified date. |
| `sortBy` | No         | The property to sort by. Set to `Natural` or `MeetingDate`. |

## Reset

## Roles

## Users
