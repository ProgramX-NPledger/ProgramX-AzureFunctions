# Application Definitions

Application Definitions are used to identify applications that may be access by a user.

Applications are assigned to Roles, which are assigned to Users. Each Application can have different Roles, though the same Application should have identical Roles even though they are stored separately. Use the API to create Roles to ensure consistency. The structure is:

Users ← Roles ← Applications

An Application Definition is required to be defined, and must implement the `IApplication` interface.

Application Definitions must provide the following features:

* Meta-data about the application
* Health check functionality
