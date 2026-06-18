# Roles

Roles are used to group users together and assign permissions to them. Roles are handled by Applications
and must be supported by the APplication for them to be effective as a means of authorization. Each Application
will have a list of Roles that are supported.

## Creating a Role

Creation of a Role is performed using the `POST api/v1/roles` endpoint with a payload which represents the Role.

```json
{
  "name": "Name of the Role",
  "description": "Description of the Role",
  "addToUsers": [
    "user-id-1",
    "user-id-2"
  ]
}
```

Where:

| Property | Description                                                                      |
| --- |----------------------------------------------------------------------------------|
| `name` | The name of the Role                                                             |
| `description` | A description of the Role. This may be `null`.                                   |
| `addToUsers` | A list of usernames to add to the Role. Leave blank if no users should be added. |

The endpoint will return a response indicating success or otherwise.

| Response | Description                                                                                                                                              |
|----------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| 201      | Created. The `Location` HTTP Header will contain the URL required to retrieve the created item. The created object will be returned in the body payload. |
| 400      | Bad Request. The reason will be returned.                                                                                                                |
| 401      | Unauthorized.                                                                                                                                            |
| 429      | Role with the same name already exists.                                                                                                                  |

## Fetching a single Role

A single role may be obtained using the `GET api/v1/roles/{role-name}` endpoint, the Role to fetch is identified by the `{role-name}` parameter.

The endpoint will return a response indicating success or otherwise.

| Response | Description                                                                                                                                              |
|----------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| 200      | OK. The requested Role will be returned in the payload.                                                                                                  |
| 401      | Unauthorized. |
| 404      | Not Found. The Role does not exist.                                                                                                                      |

If a `200` response is returned, the payload will contain a status object that looks like:

```json
{
  "role": {
    "roleName": "RoleName",
    "description": "Description of the Role"
  },
  "usersInRole": [
    "user-id-1",
    "user-id-2"
  ],
  "applicationsWithRole": [
    "application-id-1",
    "application-id-2"
  ]
}
```

Where:

| Property            | Description                                               |
|---------------------|-----------------------------------------------------------|
| `role.roleName`     | The name of the Role                                      |
| `role.description`    | A description of the Role. This may be `null`.            |
| `usersInRole`         | A list of usernames of Users who are members of the Role. |
| `applicationsWithRole` | A list of Application names that use the Role.            |

## Fetching Roles

A flexible means of querying Roles is provided using the `GET api/v1/roles` endpoint.

The following query parameters are supported (all are optional):

| Property             | Description                                                                                                  |
|----------------------|--------------------------------------------------------------------------------------------------------------|
| `continuationToken`  | If a previous query returned a `continuationToken` then it should be used to fetch the next page of results. |
| `containsText`       | Filters name and/or description by the text.                                                                 |
| `usedInApplications` | Filters Roles by the Application names that use the Role.                                                    |
| `offset`             | The offset of the first Role to return.                                                                      |
| `itemsPerPage`       | The maximum number of Roles to return within the page.                                                       |

The response will be similar to the example:

```json
{
  "items": [
    {
      "roleName": "RoleName",
      "description": "Role description"
    }
  ],
  "pagesWithUrls": [
    {
      "url": "http://localhost:8080/api/v1/roles?offset=10&itemsPerPage=10",
      "isCurrentPage": true,
      "pageNumber": 1
    }
  ],
  "continuationToken": "token",
  "itemsPerPage": 10,
  "isLastPage": false,
  "requestCharge": 1,
  "timeDeltaMs": 100,
  "totalItems": 1000
}
```

Where:

| Property | Description                                                     |
|----------|-----------------------------------------------------------------|
| `items`   | An array of Role objects.                                       |
| `pagesWithUrls` | An array of page urls which may be used to render page links.   |
| `continuationToken` | A token that can be used to retrieve the next page of results.  |
| `itemsPerPage` | The number of items per page.                                   |
| `isLastPage` | A boolean indicating whether the current page is the last page. |
| `requestCharge` | The request charge for the operation.                           |
| `timeDeltaMs` | The time delta in milliseconds for the operation.               |
| `totalItems` | The total number of items.                                      |

## Updating a Role

Updating a Role is performed using the `PUT api/v1/roles/{role-name}` endpoint with a payload which represents the Role. It may also be used to add/remove users to/from the Role. The Role to update is identified by the `{role-name}` parameter.

A typical payload would look like:

```json
{
  "description": "Description of the Role",
  "usersInRole": [
    "user-id-1",
    "user-id-2"
  ]
}
```

Where:

| Property | Description                                                                                                 |
| --- |-------------------------------------------------------------------------------------------------------------|
| `description` | A description of the Role.                                                                                  |
| `usersInRole` | A string-array of the users who should be in the Role. Leave `null` if no change to membership is required. |

The endpoint will return a response indicating success or otherwise.

| Response | Description                                                                                                                                              |
|----------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| 200      | OK. A status object will be returned in the payload.
| 400      | Bad Request. The reason will be returned.                                                                                                                |
| 401      | Unauthorized. |
| 404      | Not Found. The Role does not exist.                                                                                                                      |

If a `200` response is returned, the payload will contain a status object that looks like:

```json
{
  "name": "RoleName"
}
```

## Deleting a Role

Deleting a Role is performed using the `DELETE api/v1/roles/{role-name}` endpoint, where `{role-name}` is the name of the Role to delete.

The endpoint will return a response indicating success or otherwise.

| Response | Description                                                                                                                                              |
|----------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| 204      | OK. No content will be returned.
| 400      | Bad Request. The reason will be returned.                                                                                                                |
| 401      | Unauthorized. |
| 404      | Not Found. The Role does not exist.                                                                                                                      |

