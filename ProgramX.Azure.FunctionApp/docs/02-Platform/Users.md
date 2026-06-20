# Users

Users are the primary means of authentication in the system. Users are granted access to functions by assigning to Roles, which are defined by Applications.






## XXXXX Creating a Role

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




## Fetching a single User

A single User may be obtained using the `GET api/v1/users/{user-name}` endpoint, the User to fetch is identified by the `{user-name}` parameter.

The endpoint will return a response indicating success or otherwise.

| Response | Description                                             |
|----------|---------------------------------------------------------|
| 200      | OK. The requested User will be returned in the payload. |
| 401      | Unauthorized.                                           |
| 404      | Not Found. The User does not exist.                     |

If a `200` response is returned, the payload will contain a status object that looks like:

```json
{
  "user": {
    "userName": "username",
    "emailAddress": "email@example.com",
    "roles": [
      "role-name-1",
      "role-name-2"
    ],
    "firstName": "John",
    "lastName": "Doe"
    "profilePhotographSmall": "",
    "profilePhotographOriginal": "",
    "theme": "light",
    "createdAt": "2023-01-01T00:00:00Z",
    "updatedAt": "2023-01-01T00:00:00Z",
    "lastLoginAt": "2023-01-01T00:00:00Z",
    "lastPasswordChangedAt": "2023-01-01T00:00:00Z",
    "passwordLinkExpiresAt": "2023-01-01T00:00:00Z",
  },
}
```

Where:

| Property               | Description                                               |
|------------------------|-----------------------------------------------------------|
| `user.userName`        | The name of the User                                      |
| `user.emailAddress`    | The email address of the User                              |
| `user.roles`           | An array of Role names the User is a member of.
| `user.firstName`       | The first name of the User.                                 |
| `user.lastName`        | The last name of the User.                                  |
| `user.profilePhotographSmall` | The URL of the small profile photograph of the User. |
| `user.profilePhotographOriginal` | The URL of the original profile photograph of the User. |
| `user.theme`           | The theme of the User.                                      |
| `user.createdAt`       | The date the User was created. |
| `user.updatedAt`       | The date the User was last updated. |
| `user.lastLoginAt`     | The date the User last logged in. |
| `user.lastPasswordChangeAt` | The date the User last changed their password. |
| `user.passwordLinkExpiresAt` | The date the User's password link will expire. |

## Fetching Users

A flexible means of querying Users is provided using the `GET api/v1/users` endpoint.

The endpoint will return a response indicating success or otherwise.

| Response | Description                                                    |
|----------|----------------------------------------------------------------|
| 200      | OK.  |
| 401      | Unauthorized.                                                  |

The following query parameters are supported (all are optional):

| Property            | Description                                                                                                  |
|---------------------|--------------------------------------------------------------------------------------------------------------|
| `continuationToken` | If a previous query returned a `continuationToken` then it should be used to fetch the next page of results. |
| `containsText`      | Filters name and/or description by the text.                                                                 |
| `withRoles`         | Comma-separated list of roles that the user must be a member of (any)                                        |
| `offset`            | The offset of the first Role to return.                                                                      |
| `itemsPerPage`      | The maximum number of Roles to return within the page.                                                       |

The response will be similar to the example:

```json
{
  "items": [
    {
      "userName": "username",
      "emailAddress": "email@example.com",
      "roles": [
        "role-name-1",
        "role-name-2"
      ],
      "firstName": "John",
      "lastName": "Doe"
      "profilePhotographSmall": "",
      "profilePhotographOriginal": "",
      "theme": "light",
      "createdAt": "2023-01-01T00:00:00Z",
      "updatedAt": "2023-01-01T00:00:00Z",
      "lastLoginAt": "2023-01-01T00:00:00Z",
      "lastPasswordChangedAt": "2023-01-01T00:00:00Z",
      "passwordLinkExpiresAt": "2023-01-01T00:00:00Z",
    }
  ],
  "pagesWithUrls": [
    {
      "url": "http://localhost:8080/api/v1/users?offset=10&itemsPerPage=10",
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
| `items`   | An array of User objects.                                       |
| `pagesWithUrls` | An array of page urls which may be used to render page links.   |
| `continuationToken` | A token that can be used to retrieve the next page of results.  |
| `itemsPerPage` | The number of items per page.                                   |
| `isLastPage` | A boolean indicating whether the current page is the last page. |
| `requestCharge` | The request charge for the operation.                           |
| `timeDeltaMs` | The time delta in milliseconds for the operation.               |
| `totalItems` | The total number of items.                                      |

## Updating a User

Updating a User is performed using the `PUT api/v1/users/{user-name}/profile` endpoint.

The User to update is identified by the `{user-name}` parameter.

A typical payload would look like:

```json
{
  "emailAddress": "example@email.com",
  "firstName": "John",
  "lastName": "Doe",
  "roles": [
    "role-id-1",
    "role-id-2"
  ]
}
```

Where:

| Property       | Description                                          |
|----------------|------------------------------------------------------|
| `emailAddress` | Email Address.                                       |
| `firstName`    | User's first name.                                   |
| `lastName`     | User's last name.                                    |
| `roles`        | A string-array of the Roles the user is a member of. |

The endpoint will return a response indicating success or otherwise.

| Response | Description                                                           |
|----------|-----------------------------------------------------------------------|
| 200      | OK. A status object will be returned in the payload.                  
| 401      | Unauthorized.                                                         |
| 403      | Forbidden. A reason will be returned why the operation was forbidden. |
| 404      | Not Found. The User does not exist.                                   |

If a `200` response is returned, the payload will contain a status object that looks like:

```json
{
  "userName": "UserName"
}
```

## XXXXUpdating a User's Password


## XXXXUpdating a User's Profile Photograph


## XXXXUpdating a User's Settings


## Deleting a User

Deleting a User is performed using the `DELETE api/v1/users/{user-name}` endpoint, where `{user-name}` is the name of the User to delete.

The endpoint will return a response indicating success or otherwise.

| Response | Description                               |
|----------|-------------------------------------------|
| 204      | OK. No content will be returned.          
| 400      | Bad Request. The reason will be returned. |
| 401      | Unauthorized.                             |
| 404      | Not Found. The User does not exist.       |

