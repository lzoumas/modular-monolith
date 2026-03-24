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

### 4. Async Consumers -- BackgroundService + Integration project (single host)
**Status:** DECIDED

No separate Azure Functions project. Service Bus consumers run as `BackgroundService` in the WebApi process. The publish workflow spans multiple modules (Profiles + Brands), so the orchestration lives in a dedicated `Exhibitor.Integration` project that references only module PublicApi interfaces. Single deployment, single DI container.

Service Bus retry and dead-lettering are queue-level features -- `ServiceBusProcessor` gets them automatically via `MaxDeliveryCount`. See [06-publish-workflow](06-publish-workflow.md) for full analysis.

### 5. Cosmos DB - New database, no migration needed
**Status:** DECIDED

See [04-cosmos-setup](04-cosmos-setup.md).

### 6. Swagger / OpenAPI - Scalar
**Status:** DECIDED

FastEndpoints has built-in support for Scalar (modern OpenAPI UI). No need for Swashbuckle.

### 7. Remove Reference Monolith Demo Code - Yes
**Status:** DECIDED

Remove Shipments, Carriers, Stocks demo modules. They've served their purpose as a pattern reference. The planning docs capture the patterns.

### 8. Authentication / Authorization - Platform.Shared.ManagedIdentity
**Status:** DECIDED

Use `Platform.Shared.ManagedIdentity` for authentication. The existing microservices already use `ManagedIdentityApiClient` from `platform.shared` for service-to-service auth via Azure AD / Entra ID. The Web API will use the same package to validate bearer tokens and authenticate outbound calls. This keeps auth consistent across the platform with zero custom implementation.

---

## Open -- Needs Investigation

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
