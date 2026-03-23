# Decisions & Open Questions

Architecture decisions and remaining unknowns for the Profiles module (Phase 1).

---

## Decided

### 1. Endpoint Framework - FastEndpoints
**Status:** DECIDED

One class per endpoint, built-in FluentValidation integration, Swagger support. Eliminates the need for separate CQRS command/query handler classes -- the endpoint IS the handler for HTTP flows. Business logic that needs to be shared (e.g. publish) lives in service classes.

### 2. Result Pattern - Keep `Ardalis.Result`
**Status:** DECIDED

The current services already use `Ardalis.Result`. Zero migration effort. No functional benefit to switching to `ErrorOr`.

### 3. CQRS / Mediator - Not needed
**Status:** DECIDED

With FastEndpoints, HTTP endpoints handle their own request/response flow. Shared business logic (publish, discard draft) lives in `IProfileService` / `ProfileService`. `Platform.Shared.Mediator` is not needed in the monolith -- can be revisited later if pipeline behaviors become necessary.

### 4. Background / Event-Driven - Azure Functions (Isolated Worker)
**Status:** DECIDED

A separate `ExhibitorPlatform.Functions` project handles Service Bus triggers. It references the same module projects as the Host (no HTTP hop). Azure Functions provides automatic retry, dead-lettering, and independent scaling. See [06-background-and-event-driven](06-background-and-event-driven.md).

### 5. Cosmos DB - New database, no migration needed
**Status:** DECIDED

Greenfield -- no production data exists. New `ExhibitorPlatformDb` database with module-owned containers. Cosmos DB emulator for local dev. See [04-data-migration](04-data-migration.md).

### 6. Swagger / OpenAPI - Scalar
**Status:** DECIDED

FastEndpoints has built-in support for Scalar (modern OpenAPI UI). No need for Swashbuckle.

### 7. Remove Reference Monolith Demo Code - Yes
**Status:** DECIDED

Remove Shipments, Carriers, Stocks demo modules. They've served their purpose as a pattern reference. The planning docs capture the patterns.

---

## Open -- Needs Investigation

### 8. Authentication / Authorization
**Status:** TODO

Current Function Apps likely use Function Keys or Azure AD / Entra ID. The Web API will need bearer token auth. Investigate what the current services use and replicate.

### 9. Hosting: App Service vs Container App
**Status:** TODO

| Option | Notes |
|---|---|
| **Azure App Service** | Simple, familiar, easy scaling |
| **Azure Container Apps** | Docker-based, better for complex deployments |

### 10. External HTTP Calls
**Status:** TODO

Search the Profile service for `HttpClient` / `ManagedIdentityApiClient` usage. These are calls to external services and will need to be preserved in the monolith.

---

## Tracking

- `TODO` -- Not started
- `IN PROGRESS` -- In progress
- `DECIDED` -- Decision made
