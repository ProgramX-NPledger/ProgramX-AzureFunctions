# HTTP Triggers

Contains HTTP Triggers for the ProgramX Azure Function App.

## Roles HTTP Trigger

Handles HTTP requests related to roles management.

### Updating Roles

Updates roles in the database.

Triggers on `PUT /role/(role-name)`

- **Authorization**:
  - Bearer Token
- **Path Parameters**:
  - `role-name`: The name of the role to be updated.
- **Request Body**:
  - `name`: The new name for the role.
  - `description`: The new description for the role.
  - `usersInRole`: An array of usernames in the role. A diff will be taken and users will be added/removed as needed. If `null`, no changes are made to users.
  - `applications`: An array of application names associated with the role.

- Returns:
  - 200 OK if successful.
  - 400 Bad Request if the request body is invalid.
  - 401 Unauthorized if the user is not authenticated.
  - 403 Forbidden if the user does not have permission to update roles.
  - 404 Not Found if the role does not exist.
  - 500 Server error
