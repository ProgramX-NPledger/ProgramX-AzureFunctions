# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository layout gotcha

Three nested directories share almost the same name. Get this right before running anything:

```
ProgramX-AzureFunctions/                     <- git root (README, LICENSE, .github)
└── ProgramX.Azure.FunctionApp/              <- solution dir; .sln and docs/ live here
    ├── ProgramX.Azure.FunctionApp/          <- the Function App project itself
    ├── ProgramX.Azure.FunctionApp.Tests/
    ├── ProgramX.Azure.FunctionApp.{Core,Contract,Model,Cosmos,AzureStorage,Osm,Scouting,AzureCommunications}/
    ├── docs/
    └── HTTP Requests/
```

All `dotnet` commands below assume cwd is the **solution dir** (the middle level).

## Commands

```bash
# Build (verified working)
dotnet build ProgramX.Azure.FunctionApp.sln
dotnet build ProgramX.Azure.FunctionApp/ProgramX.Azure.FunctionApp.csproj   # app only

# Test
dotnet test ProgramX.Azure.FunctionApp.Tests/ProgramX.Azure.FunctionApp.Tests.csproj
dotnet test --filter "Category=Unit"                       # exclude integration
dotnet test --filter "Category!=Integration"
dotnet test --filter "Category=UsersHttpTrigger"           # one trigger's suite
dotnet test --filter "FullyQualifiedName~CreateUser"       # one test/class
dotnet test --collect:"XPlat Code Coverage"                # coverlet.collector is referenced

# Run locally — port 7276 is set in Properties/launchSettings.json
cd ProgramX.Azure.FunctionApp
func start --pause-on-error --port 7276 --cors http://localhost:4200
```

### Known-broken state at HEAD

The **test project does not compile**. One error:

```
Mocks/UsersHttpTriggerBuilder.cs(62,20): error CS7036: no argument given for required
parameter 'multiPartContentHandler' of UsersHttpTrigger(..., MultiPartContentHandler)
```

The app project builds clean. HEAD is mid-refactor (see commit `8814e9c`, "Azure File Storage rework"), and `UsersHttpTrigger` gained a `MultiPartContentHandler` ctor parameter that the test builder wasn't updated for. `dotnet build` on the **solution** therefore fails — build the app project alone if you only need the app. Fix the builder before trusting any test run.

### Local dependencies

- **Azurite** must be running for blob/queue/table (`UseDevelopmentStorage=true` → ports 10000/10001/10002). Launch: `azurite --location <workspace>` (npm global shim is `azurite.cmd`; the package's `dist/src/main.js` is *not* the CLI entrypoint — `dist/src/azurite.js` is).
  - **Requires Azurite ≥ 3.37.0.** `Azure.Storage.Blobs` 12.27.0 defaults to `ServiceVersion.V2026_02_06` and sends `x-ms-version: 2026-02-06`; Azurite only accepted that from 3.37.0 onwards (3.35.0 capped at `2025-11-05`). An older emulator rejects **every** blob call with `400 InvalidHeaderValue` while the same code works against real Azure. Upgrade with `npm install -g azurite@latest`; verify with `azurite --version` and by checking `dist/src/blob/utils/constants.js` → `ValidAPIVersions` contains the version the SDK sends. Bumping `Azure.Storage.Blobs` can re-break this — the SDK's newest `ServiceVersion` enum member is what goes on the wire unless `BlobClientOptions` pins it. Azurite 3.37.0 needs Node ≥ 22.
- **Cosmos DB Emulator** at `https://localhost:8081` per `appsettings.test.json`.
- `local.settings.json` is committed with `"IsEncrypted": true` — values are ciphertext. Use `func settings decrypt` before reading or editing them.
- Real local config (Cosmos connection string, `JwtKey`, OSM secrets) lives in `ProgramX.Azure.FunctionApp/appsettings.Development.json`, which **is** in git. Treat those as live credentials.

## Architecture

**.NET 8 isolated-worker Azure Functions** (`Microsoft.Azure.Functions.Worker` v2, `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`). All HTTP routes are prefixed `api/v1` via `host.json` → `extensions.http.routePrefix`.

### Composition root

`ProgramX.Azure.FunctionApp/Program.cs` is where essentially all DI lives — repositories, clients, health checks, the OSM `HttpClient` with its `AuthTokenHandler`. Note that `ProgramX.Azure.FunctionApp.Core/DependencyInjectionConfiguration.cs` is called first but registers only `ObjectSerializer`; don't go looking there for service wiring.

Two secrets bypass `IConfiguration` entirely and are read with `Environment.GetEnvironmentVariable`, throwing at startup if unset:

- `CosmosDBConnection` → `CosmosClient`
- `AzureWebJobsStorage` → `BlobServiceClient`

Repositories are registered as **singletons** wrapping the shared `CosmosClient`.

### Project dependency shape

`Contract` holds every interface (`IUserRepository`, `IStorageClient`, `IApplication`, …) and `Model` holds entities/DTOs/criteria; both are leaf projects. `Cosmos`, `AzureStorage`, `AzureCommunications`, `Osm` are the infrastructure implementations. `Scouting` is a feature module. The `FunctionApp` project references all of them and owns the HTTP triggers. Tests reference only `FunctionApp`, `Contract`, and `Model`.

### HTTP trigger pattern

Every authenticated trigger derives from `HttpTriggers/AuthorisedHttpTriggerBase` and wraps its body in a continuation delegate:

```csharp
[Function(nameof(GetRole))]
public async Task<HttpResponseData> GetRole(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "roles/{roleName?}")] HttpRequestData req,
    string? roleName)
    => await RequiresAuthentication(req, requiredAnyOfRoles: null,
        async (username, roles) => { /* body */ });
```

`AuthorizationLevel.Anonymous` is used throughout — auth is **not** delegated to the Functions host. `RequiresAuthentication` parses the `Authorization: Bearer` header itself against `Configuration["JwtKey"]` (must be ≥64 chars; changing it invalidates all tokens), validates the token, checks role membership, and short-circuits to 400/401/500 via `HttpResponseDataFactory`. Pass `permitAnonymous: true` to skip all of it.

Triggers by area: `Users`, `Roles`, `Applications` (platform/RBAC); `Login`, `Reset`, `BuildInformation`, `HealthCheck` (cross-cutting); `Files` (blob storage + image resizing); `OsmIntegration`, `ScoutingActivities`, `ScoresLedger` (Scouting feature, routes under `scouts/`).

### RBAC model

`Users ← Roles ← Applications`. Applications publish role names; roles are assigned to users; permissions are checked per-application-per-function. See `docs/02-Platform/README.md`.

### Application definitions (extensibility seam)

An "Application" is a pluggable feature module implementing `IApplication` — supplying `ApplicationMetaData` (name, friendly name, `RequiresRoleNames`, target URL) and a set of `IApplicationHealthCheck`s. Examples: `ApplicationDefinitions/Administration/AdministrationApplication.cs` and `ProgramX.Azure.FunctionApp.Scouting/Application.cs`.

`CachingApplicationProvider` discovers them **by reflection** (10-minute cache), scanning only `Assembly.GetExecutingAssembly()` and `Assembly.GetCallingAssembly()` — there's a `TODO discover all assemblies`. Constructor args are resolved by picking the ctor with the most parameters and pulling each from `IServiceProvider`, so an `IApplication`'s dependencies must already be registered in `Program.cs`.

Note: `appsettings.Development.json` has an `Applications` array of assembly-qualified type names. **No code reads it** — it's vestigial. Discovery is purely reflective.

### Cosmos DB conventions

Per `docs/01-Getting-started/Development-principles.md`, and enforced in `Program.cs` via `CosmosPropertyNamingPolicy.CamelCase`:

- Cosmos's `id` property is an internal detail — **never** expose it to consumers. Callers address entities by a unique key that doubles as the partition key.
- Every entity carries `id`, `createdAt`, `updatedAt`, `schemaVersionNumber`, `type`.
- Database and container names plus partition key paths are centralised in `Cosmos/DatabaseNames.cs` and `Cosmos/ContainerNames.cs` (databases `core` and `scouting`).
- Paging goes through `CosmosPagedReader` / `CosmosPagedResult` with continuation tokens; triggers read `continuationToken`, `offset`, `itemsPerPage` from the query string.

### Blob storage layout

Files live at `(purpose)/(filename)/original.ext`, with resized variants as `(purpose)/(filename)/[wNN][hNN].ext` and a sibling `blobIndexEntry.json` recording the original filename and the roles required to read it. Resizing is on-demand-and-cached via `SixLabors.ImageSharp`. Full spec in `docs/01-Getting-started/File-Storage.md`.

## Conventions to follow

From `docs/01-Getting-started/Development-principles.md` — these are project rules, not suggestions:

- Cross API boundaries with **DTOs** only, never entities.
- Reach infrastructure through **interfaces** in `Contract`, for testability.
- Target **≥80% test coverage**.
- **Let exceptions bubble** to the HTTP trigger. Do not log in repository/service layers — log at the entry point only.
- Requests and responses are strongly typed.
- Cosmos specifics stay hidden behind the repository layer.
- API content type is `application/json`.

Documentation is treated as load-bearing: the principles file opens by noting development is intermittent and must survive long gaps, so keep `docs/` current when changing behaviour.

## Testing conventions

NUnit 3 + Moq + FluentAssertions. `TestBase` provides pre-wired `Mock<CosmosClient>`/`Mock<Container>`/`Mock<ILogger>` and loads `appsettings.test.json` (copied to output). Trigger tests use fluent builders in `Tests/Mocks/` (`UsersHttpTriggerBuilder`, `RolesHttpTriggerBuilder`, …) plus hand-rolled `TestHttpRequestData`/`TestHttpResponseData` fakes, since the isolated worker's `HttpRequestData` is awkward to mock directly.

Tests are organised one class per operation (`UsersHttpTriggerCreateUserTests`, `…DeleteUserTests`) and tagged with layered `[Category]` attributes: a tier (`Unit`, `Integration`), a subsystem (`Cosmos`, `Azure`, `HttpTrigger`), and an operation (`CreateUser`, `GetRole`). Filter on these rather than on namespaces.

## Deployment

`.github/workflows/main_fa-programx(staging).yml` builds on push to `main` and deploys the **same package to both the `staging` and `production` slots** of function app `fa-programx` — there is no gated promotion. It also stamps `Commit__CommitHash`, `Commit__BuildNumber`, and `Commit__DeployedAt` into app settings, surfaced by `BuildInformationHttpTrigger` at `GET api/v1/build`.

CORS headers are not emitted by the Functions host by default; they must be configured explicitly (`--cors` locally, Function App → API → CORS in Azure). See `ProgramX.Azure.FunctionApp/README.md`.

## Stale documentation — do not trust these

- `docs/01-Getting-started/Architecture.md` claims the back-end uses **Node.js** and that the database is **Cosmos DB with MongoDB**. Both are wrong: it's .NET 8 isolated-worker Functions against the Cosmos **SQL/NoSQL** API via `Microsoft.Azure.Cosmos`.
- The CI workflow sets `DOTNET_VERSION: '9.0.x'` while every csproj targets `net8.0`.
- Installed SDKs on this machine are 8.0.416 and 10.0.101 (no 9.x).

## Housekeeping

Azurite emulator artifacts (`__azurite_db_*.json`, `__blobstorage__/`, `__queuestorage__/`, `AzuriteConfig`) get written into whatever directory Azurite is launched from and are currently **untracked but not ignored** in the solution dir. They're emulator state, not source — add them to `.gitignore` rather than committing them.
