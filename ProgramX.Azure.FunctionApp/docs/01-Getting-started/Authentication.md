# Authentication

Authorisation of calls is handled using a Bearer token, which is passed in the `Authorization` header. This token
is provided using the `POST api/v1/login` endpoint.

## Log in

Log in to the API to retrieve the Bearer token. This token can then be used in subsequent requests to authenticate the user.

Use the `POST api/v1/login` endpoint to retrieve a Bearer token with the credentials in the payload.

```json
{
    "userName": "admin",
    "password": "passwo0rd"
}
```

This will return a JSON object with the Bearer token.

| Response | Description                 |
|----------|-----------------------------|
| `200`    | Auhentication is successful |
| `401`    | Invalid credentials         |

Successful authentication will return a JSON payload:

```json
{
  "token": "(bearer-token)",
  "userName": "admin",
  "emailAddress": "admin@example.com",
  "roles": [
    "reader",
    "writer"
  ],
  "applications": [
    {
      "name": "application-name",
      "friendlyName": "My Application",
      "isDefaultApplicationOnLogin": true,
      "ordinal": 1,
    }
  ],
  "profilePhotoBase64": "(base64-encoded-image)",
  "firstName": "John",
  "lastName": "Doe",
  "initials": "JD"
}
```

Where:

| Field | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
|-------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `token` | The Bearer token which must be used in all subsequent authenticated requests.                                                                                                                                                                                                                                                                                                                                                                                               |
| `userName` | The user's username.                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| `emailAddress` | The user's email address.                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| `roles` | The user's roles.                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| `applications` | The user's applications (according to permissions). The `name` of the application is the 'slug' of the application which can be used in the URL. eg. `https://myserver.com/application-name` There must be a user interface capable of responding to this route. When displayed in a menu, the menu should be sorted by the `ordinal`. When logging in, the user should be immediately sent to the first application which has `isDefaultApplicationOnLogin` set to `true`. |
| `profilePhotoBase64` | The user's profile photo as a base64-encoded image.                                                                                                                                                                                                                                                                                                                                                                                                                         |
| `firstName` | The user's first name.                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| `lastName` | The user's last name.                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| `initials` | The user's initials.                                                                                                                                                                                                                                                                                                                                                                                                                                                        |

## Authenticated calls

All subsequent calls to the API must include the Bearer token in the `Authorization` header.

For example:

```json
GET https://myserver.com/api/v1/application/new-application
Authorization: Bearer (bearer-token)
```

This may return a response:

| Response | Description                                                                            |
|----------|----------------------------------------------------------------------------------------|
| `400`    | Verify in logs. May be due to the `Authorization` header not being provided.           |
| `401`    | If the Bearer token is invalid or the requested operation is not permitted.            |
| `500`    | Verify in logs. May be beause the required JWT Key was not specified in configuration. |

## Configuration

A JWT Key must be specified in the configuration file `appsettings.json`.

```json
{
  "JwtKey": "any-long-and-boring-key-that-is-long-and-boring-is-it-long-and-boring-or-is-it-not-long-and-boring"
}
```
The JWT key must be at least 64 characters long. Changing this will invalidate all existing Bearer tokens.

## Log out

Log out of the API to invalidate the Bearer token.
