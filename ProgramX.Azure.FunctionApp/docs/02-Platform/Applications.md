# Applications

Applications are sections of related functionality. They are defined by the .NET application and are referred to by their name. They are not stored and their use is limited to the existing API.

## Fetching a single Application

A single Application can be obtained using the `GET api/v1/applications/{applicationName}` endpoint, where `{applicationName}` is the name of the Application.

The endpoint will return a response indicating success or otherwise.

| Response | Description                                                    |
|----------|----------------------------------------------------------------|
| 200      | OK. The requested Application will be returned in the payload. |
| 401      | Unauthorized.                                                  |
| 404      | Not Found. The Application does not exist.                     |
| 500      | Internal Server Error. An unexpected error occurred.           |

If a `200` response is returned, the payload will contain a status object that looks like:

```json
{
  "name": "Application Name",
  "description": "Application Description",
  "friendlyName": "Application Friendly Name",
  "imageUrl": "https://example.com/image.png",
  "targetUrl": "https://example.com",
  "requiresRoleNames": ["Role1", "Role2"]
}
```

Where:

| Property              | Description                                                                |
|-----------------------|----------------------------------------------------------------------------|
| `name`                | The name of the Application.                                               |
| `friendlyName`        | A friendly name for the Application. This may be used in menus/navigation. |
| `description`         | A description of the Application. This may be `null`.                      |
| `imageUrl`            | A URL to an image that represents the Application. This may be `null`.     |
| `targetUrl`           | A URL to the Application.                                                  |
| `requiresRoleNames`   | A list of Role names that the Application requires.   |   

## Fetching Applications

Application information can be obtained by using the API, which is particularly useful when determining which Applications are available for use for a particular user.

The following query parameters are supported (all are optional):

| Property             | Description                                                 |
|----------------------|-------------------------------------------------------------|
| `containsText`       | Filters name, friendly-name and/or description by the text. |
| `supportsRoles` | Filters by supported Roles.                                 |

**Use `supportsRoles` to obtain a list of supported Applications for the User's Roles.**
The endpoint will return a response indicating success or otherwise.

| Response | Description                                                    |
|----------|----------------------------------------------------------------|
| 200      | OK.  |
| 401      | Unauthorized.                                                  ||

The response will be similar to the example:

If a `200` response is returned, the payload will contain a status object that looks like:

```json
{
  "applications": [
    {
      "name": "Application Name",
      "description": "Application Description",
      "friendlyName": "Application Friendly Name",
      "imageUrl": "https://example.com/image.png",
      "targetUrl": "https://example.com",
      "requiresRoleNames": ["Role1", "Role2"]
    }
  ]
}

```

Where:

| Property              | Description                                                                |
|-----------------------|----------------------------------------------------------------------------|
| `name`                | The name of the Application.                                               |
| `friendlyName`        | A friendly name for the Application. This may be used in menus/navigation. |
| `description`         | A description of the Application. This may be `null`.                      |
| `imageUrl`            | A URL to an image that represents the Application. This may be `null`.     |
| `targetUrl`           | A URL to the Application.                                                  |
| `requiresRoleNames`   | A list of Role names that the Application requires.   |

## Performing an Application health check

Applications can support a health check API which may be used to establish the health and status of the Application and indicate if further configuration is necessary, some of which may be supported by the API.

An Application health-check may be peformed using the `GET api/v1/applications/{applicationName}/health` endpoint.

The endpoint will return a response indicating success or otherwise.

| Response | Description                                                    |
|----------|----------------------------------------------------------------|
| 200      | OK.  |
| 401      | Unauthorized.                                                  |
| 404      | Not Found. The Application does not exist.                     |
| 500      | Internal Server Error. An unexpected error occurred.           |

If a `200` response is returned, the payload will contain a status object that looks like:

```json
{
  "name": "Application halth-check Name",
  "timeStamp": "2023-01-01T00:00:00Z",
  "isHealthy": true,
  "message": "Application is healthy",
  "items": [
    "isHealthy": true,
    "message": "Item is healthy",
    "healthCheckName": "item-check1",
    "friendlyName": "Item 1"
   }
  ]
}
```

Where:

| Property                  | Description                                                                       |
|---------------------------|-----------------------------------------------------------------------------------|
| `name`                    | The name of the Application.                                                      |
| `tiemStamp`               | Timestamp of the health-check.                                                    |
| `message`                 | A message from the health-check.                                                  |
| `imageUrl`                | A URL to an image that represents the Application. This may be `null`.            |
| `items`                   | If the health-check comprises of multiple checks, individual checks are returned. |
| `items[].isHealthy`       | Sub-item health status.                                                           |   
| `items[].message`         | Sub-item message.                                                                 |   
| `items[].healthCheckName` | Sub-item health check name.                                                       |   
| `items[].friendlyName`    | Sub-item health check friendly name.                                              |   
