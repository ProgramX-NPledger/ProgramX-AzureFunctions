# Application Definitions

Application Definitions are used to identify applications that may be accessed by a user.

Application Definitions must implement the `IApplication` interface, which provides enough information for the any UI shell to determine permissions and navigational requirements.

The `GetApplicationMetaData()` method provides the following information in an `ApplicationMetaData` object:

| Property | Type | Description | Example |
|---|---|---|---|
| `Name` | String | Name of the application. This should be unique and is not necessarily displayed to the user. | `scouting` |
| `FriendlyName` | String | Friendly name of the application, which may be presented to the user. | `Scouting` |
| `RequiresRoleNames` | String array | List of Roles the application requires for access. The application may define more. | `["scouting"]` |
| `TargetUrl` | String | The URL (with leafing `/` character) for the shell to use for navigation. | `/scouting` |
| `Description` | String | The description of the application. | `An application` |
| `ImageUrl` | String | URL to an image to represent the application. | `/image.png` |


