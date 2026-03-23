# Decisions & Open Questions

Architecture decisions and remaining unknowns for the Profiles module (Phase 1).

---

## Decided

### 1. Endpoint Framework ? FastEndpoints
**Status:** ? Decided

One class per endpoint, built-in FluentValidation integration, Swagger support. Eliminates the need for separate CQRS command/query handler classes — the endpoint IS the handler for HTTP flows. Business logic that needs to be shared (e.g. publish) lives in service classes.

### 2. Result Pattern ? Keep `Ardalis.Result`
**Status:** ? Decided

The current services already use `Ardalis.Result`. Zero migration effort. No functional benefit to switching to `ErrorOr`.

### 3. CQRS / Mediator ? Not needed
**Status:** ? Decided

With FastEndpoints, HTTP endpoints handle their own request/response flow. Shared business logic (publish, discard draft) lives in `IProfileService` / `ProfileService`. `Platform.Shared.Mediator` is not needed in the monolith — can be revisited later if pipeline behaviors become necessary.

### 4. Background / Event-Driven ? Azure Functions (Isolated Worker)
**Status:** ? Decided

A separate `ExhibitorPlatform.Functions` project handles Service Bus triggers. It references the same module projects as the Host (no HTTP hop). Azure Functions provides automatic retry, dead-lettering, and independent scaling. See [06-background-and-event-driven](06-background-and-event-driven.md).

### 5. Cosmos DB ? Shared database, keep existing containers
**Status:** ? Decided

Same Cosmos DB instance, same containers, same partition keys. No data migration needed for Phase 1.

- `profiles` ? `/exhibitorId`

### 6. Swagger / OpenAPI ? Scalar
**Status:** ? Decided

FastEndpoints has built-in support for Scalar (modern OpenAPI UI). No need for Swashbuckle.

### 7. Remove Reference Monolith Demo Code ? Yes
**Status:** ? Decided

Remove Shipments, Carriers, Stocks demo modules. They've served their purpose as a pattern reference. The planning docs capture the patterns.

---

## Open — Needs Investigation

### 8. Authentication / Authorization
**Status:** ?? Needs investigation

Current Function Apps likely use Function Keys or Azure AD / Entra ID. The Web API will need bearer token auth. Investigate what the current services use and replicate.

### 9. Hosting: App Service vs Container App
**Status:** ?? Needs decision

| Option | Notes |
|---|---|
| **Azure App Service** | Simple, familiar, easy scaling |
| **Azure Container Apps** | Docker-based, better for complex deployments |

### 10. External HTTP Calls
**Status:** ?? Needs investigation

Search the Profile service for `HttpClient` / `ManagedIdentityApiClient` usage. These are calls to external services and will need to be preserved in the monolith.

### 11. Production Cutover Strategy
**Status:** ?? Needs planning

When do we cut over the Profiles service? Options:
- **Big bang:** Deploy monolith, switch DNS, decommission old Function App
- **Parallel run:** Both old and new running, traffic migration via feature flags

---

## Tracking

- ?? Not started
- ?? In progress
- ? Decided / Implemented
