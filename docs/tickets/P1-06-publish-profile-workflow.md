---
name: Feature Request
about: Publish Profile workflow -- endpoint, Service Bus message, Azure Function
title: '[Feature] Publish Profile workflow'
labels: enhancement, phase-1, profiles, publish-workflow
assignees: ''
---

## Summary

Implement the full publish workflow: a FastEndpoints endpoint that sends a Service Bus message (returns 202 Accepted), and an Azure Function that receives the message, publishes the profile via `IProfileModuleApi`, transforms the result, and sends it to an external system.

## Prerequisites

- P1-04 (Service Layer) -- `IProfileService.PublishAsync()` must be implemented
- P1-03 (PublicApi) -- `IProfileModuleApi` and `PublishProfileMessage` must exist
- P0-03 (Functions) -- Functions project must be set up

## Background / Context

The publish workflow is the most architecturally significant feature. It demonstrates the split between the API Host (HTTP) and Azure Functions (async processing), both sharing the same module code. The endpoint is a thin sender; the Function is the orchestrator. See [06-background-and-event-driven.md](../planning/06-background-and-event-driven.md) for full details.

**Flow:**
```
POST /api/profiles/{exhibitorId}/{profileId}/publish
    -> Endpoint sends PublishProfileMessage to Service Bus queue
    -> Returns 202 Accepted

Service Bus "profile-publish" queue
    -> PublishProfileFunction picks up message
    -> Step 1: IProfileModuleApi.PublishAsync() -- draft to published (module logic)
    -> Step 2: Transform published profile for external system (integration logic)
    -> Step 3: HTTP POST to external system (integration logic)
    -> On failure: message retried automatically, dead-lettered after max attempts
```

## Requirements

- [ ] Publish endpoint sends a message and returns 202 (fire-and-forget from the client's perspective)
- [ ] Function processes the message and calls module PublicApi
- [ ] Function transforms the result and sends to external system
- [ ] Failure in the Function causes automatic retry via Service Bus
- [ ] The module (ProfileService) has no knowledge of external systems

## Implementation Tasks

### Sub-task 1: Publish Endpoint
- [ ] Create `Features/PublishProfile/PublishProfileEndpoint.cs`:
  - Inherits `EndpointWithoutRequest`
  - Route: `Post("/api/profiles/{exhibitorId}/{profileId}/publish")`
  - Inject `ServiceBusSender` (for the `profile-publish` queue)
  - Build `PublishProfileMessage` from route params + `User.Identity.Name`
  - Send message via `ServiceBusSender.SendMessageAsync()`
  - Return `202 Accepted`

### Sub-task 2: Service Bus Configuration
- [ ] Register `ServiceBusClient` in Host DI (from `Azure.Messaging.ServiceBus` NuGet)
- [ ] Register named `ServiceBusSender` for the `profile-publish` queue
- [ ] Add NuGet package `Azure.Messaging.ServiceBus` to the Features project (or Host)
- [ ] Add `ServiceBus:ConnectionString` and `ServiceBus:ProfilePublishQueueName` to app settings

### Sub-task 3: Azure Function
- [ ] Create `ExhibitorPlatform.Functions/Functions/Profiles/PublishProfileFunction.cs`:
  - Inject `IProfileModuleApi`, `IHttpClientFactory`, `ILogger<PublishProfileFunction>`
  - `ServiceBusTrigger` on `profile-publish` queue
  - Deserialize `PublishProfileMessage` from `ServiceBusReceivedMessage.Body`
  - Step 1: Call `_profileApi.PublishAsync(exhibitorId, profileId, publishedBy)`
  - Step 2: If success, transform `PublishedProfile` to `ExternalProfilePayload`
  - Step 3: Send via `IHttpClientFactory.CreateClient("ExternalCatalogApi").PostAsJsonAsync()`
  - On failure: log error and throw (triggers Service Bus retry)
- [ ] Create `ExhibitorPlatform.Functions/Models/ExternalProfilePayload.cs`:
  - Flattened payload for the external system
  - Properties: `ExhibitorId`, `CompanyName`, `Contact` (first/last/email/phone), `Website`, `PublishedAt`
- [ ] Register `IHttpClientFactory` with named client `ExternalCatalogApi` in Functions `Program.cs`
- [ ] Add `ExternalCatalogApi:BaseUrl` to Functions app settings

### Sub-task 4: Functions DI Wiring
- [ ] Add project reference to `Exhibitor.Profiles.PublicApi` in Functions `.csproj`
- [ ] Verify `AddProfilesModule()` registers `IProfileModuleApi` (so the Function can resolve it)
- [ ] Configure Service Bus connection in Functions `local.settings.json`:
  - `ServiceBusConnection` connection string

## Acceptance Criteria

- [ ] `POST /api/profiles/{exhibitorId}/{profileId}/publish` returns 202 Accepted
- [ ] A `PublishProfileMessage` appears on the `profile-publish` Service Bus queue
- [ ] The Function picks up the message and calls `IProfileModuleApi.PublishAsync()`
- [ ] On successful publish, the profile's `Published` content is populated and `PublishedOn`/`PublishedBy` are set
- [ ] The Function transforms the result and sends HTTP POST to the external system
- [ ] On Function failure, the message is retried (not lost)
- [ ] The Profiles module (`ProfileService`) has no references to external systems
- [ ] Endpoint visible in Scalar UI at `/swagger`

## Files to Modify

| File | Change Type |
|------|-------------|
| `Profiles/Exhibitor.Profiles.Features/Features/PublishProfile/PublishProfileEndpoint.cs` | Add |
| `ExhibitorPlatform.Functions/Functions/Profiles/PublishProfileFunction.cs` | Add |
| `ExhibitorPlatform.Functions/Models/ExternalProfilePayload.cs` | Add |
| `ExhibitorPlatform.Host/Program.cs` | Update (Service Bus DI) |
| `ExhibitorPlatform.Host/appsettings.json` | Update (Service Bus config) |
| `ExhibitorPlatform.Functions/Program.cs` | Update (HttpClientFactory, module DI) |
| `ExhibitorPlatform.Functions/local.settings.json` | Update (Service Bus connection) |
| `ExhibitorPlatform.Functions/ExhibitorPlatform.Functions.csproj` | Update (project refs, NuGets) |

## Out of Scope

- Dead-letter queue monitoring/alerting
- Service Bus topic/subscription (using a simple queue for Phase 1)
- Retry policy customization beyond Service Bus defaults

## Technical Notes

- **Separation of concerns:** The module does the business logic (draft -> published). The Function does the integration work (transform, send to external). The module never knows about external systems.
- **ServiceBusSender injection:** Register in DI using `ServiceBusClient.CreateSender("profile-publish")`. The sender is thread-safe and should be a singleton.
- **ExternalCatalogApi:** The external system URL comes from config. Use `IHttpClientFactory` named client pattern. The actual external system API contract should be confirmed -- the `ExternalProfilePayload` model in the planning doc is illustrative.
- **For Phase 1, keep orchestration logic in the Function.** Extract to a `ProfilePublishOrchestrator` service only if it gets complex (see 06-background-and-event-driven.md).

## Verification Steps

1. Start the Host and Functions projects
2. Create a profile via `POST /api/profiles/{exhibitorId}`
3. Publish via `POST /api/profiles/{exhibitorId}/{profileId}/publish` -> 202
4. Check Function logs -- message received and processed
5. Get the profile -- `Published` content should be populated
6. Verify external HTTP call was made (use a mock or test endpoint)
7. Test failure: stop the external system, publish again, verify the message is retried
