# Consolidation Overview

## Goal

Consolidate three separate Azure Functions microservices into a single **ASP.NET Core Modular Monolith** using vertical-slice architecture, following the patterns established in this repository.

### Phase 1 Scope: Profiles Only

Phase 1 builds the full architecture with the **Profiles module** -- API endpoints (FastEndpoints) + publish workflow (BackgroundService + Integration project). Once the architecture is proven, Brands follows the same pattern in Phase 2.

## Source Repositories

| Repo | Branch | Purpose | Runtime | Database |
|---|---|---|---|---|
| [experience.exhibitor.profile.service](https://github.com/innovationsandmore/experience.exhibitor.profile.service) | `develop` | Exhibitor profile management (CRUD, media, contacts, showroom) | Azure Functions (Isolated Worker) .NET 10 | Cosmos DB |
| [experience.exhibitor.brands.service](https://github.com/innovationsandmore/experience.exhibitor.brands.service) | `develop` | Brand catalog, exhibitor brands, brand requests, file uploads | Azure Functions (Isolated Worker) .NET 10 | Cosmos DB |
| [experience.exhibitor.shared.service](https://github.com/innovationsandmore/experience.exhibitor.shared.service) | `main` | Shared library -- BaseEntity, Cosmos DB client/config/repositories, health checks, test fixtures | Class library .NET 10 | -- |

## External Dependency (unchanged)

| Repo | Purpose |
|---|---|
| [platform.shared](https://github.com/innovationsandmore/platform.shared) | Cross-cutting: Mediator (ICommand/IQuery CQRS), Azure Functions helpers, file storage, service bus, managed identity API client, extensions |

`platform.shared` stays as-is. The new monolith will reference the libraries it needs (e.g. `Platform.Shared.FileStorage`, `Platform.Shared.ServiceBus`) as project references or NuGet packages. `Platform.Shared.Mediator` is not needed -- FastEndpoints handles HTTP dispatch and service classes handle shared business logic.

## Key Architecture Changes

| Concern | Current (microservices) | Target (modular monolith) |
|---|---|---|
| **Host** | 2x Azure Functions apps | 1x ASP.NET Core Web API (single deployment) |
| **Endpoints** | Azure Functions HTTP triggers | FastEndpoints (one class per endpoint) |
| **Business logic** | `Platform.Shared.Mediator` handlers | Service classes (`IProfileService`) -- no mediator needed |
| **Validation** | FluentValidation | FluentValidation (same, built into FastEndpoints) |
| **Error handling** | `Ardalis.Result` | `Ardalis.Result` (unchanged) |
| **Database** | Cosmos DB | Cosmos DB (same containers, same partition keys) |
| **Shared code** | `Exhibitor.Shared.*` NuGet/project refs | Absorbed into `Common/` projects |
| **Event-driven** | Service Bus triggers in Function App | `BackgroundService` + `ServiceBusProcessor` in WebApi process |
| **Cross-module calls** | HTTP calls between Function apps | In-process via `PublicApi` interfaces (no network hop) |
| **Cross-module orchestration** | N/A | `Exhibitor.Integration` project (publish workflow) |

## Target Solution Structure

### Solution Explorer View

```
Solution 'ExhibitorPlatform'
+-- src (solution folder)
|   +-- ExhibitorPlatform.WebApi                     # ASP.NET Core Web API (single host)
|   +-- Integration (solution folder)
|   |   +-- Exhibitor.Integration                    # Publish workflow orchestration
|   +-- Modules (solution folder)
|       +-- Profiles (solution folder)
|       |   +-- Exhibitor.Profiles.Domain
|       |   +-- Exhibitor.Profiles.Features
|       |   +-- Exhibitor.Profiles.Infrastructure
|       |   +-- Exhibitor.Profiles.PublicApi
|       +-- Common (solution folder)
|           +-- Exhibitor.Common.Application
|           +-- Exhibitor.Common.Cosmos
|           +-- Exhibitor.Common.Cosmos.Testing
+-- tests (solution folder)
    +-- Modules (solution folder)
        +-- Profiles (solution folder)
            +-- Exhibitor.Profiles.Tests.Unit
            +-- Exhibitor.Profiles.Tests.Integration
```

> **Phase 2** adds `Brands` as a sibling solution folder under `Modules`.

### Full Disk Layout

```
repo-root/
|
|-- src/
|   |-- ExhibitorPlatform.WebApi/
|   |   |-- Program.cs                                 # DI wiring, middleware, Cosmos, health checks
|   |   |-- ExhibitorPlatform.WebApi.csproj
|   |   |-- appsettings.json
|   |   |-- appsettings.Development.json
|   |   +-- appsettings.{env}.json                     # dev, qa, uat, prod
|   |
|   |-- Integration/
|   |   +-- Exhibitor.Integration/
|   |       |-- Exhibitor.Integration.csproj
|   |       |-- DependencyInjection.cs                 # AddIntegrationWorkflows()
|   |       |-- Features/
|   |       |   +-- PublishExhibitor/
|   |       |       +-- PublishExhibitorEndpoint.cs     # POST /api/exhibitors/{id}/publish -> SB msg
|   |       |-- Workers/
|   |       |   +-- ExhibitorPublishWorker.cs           # BackgroundService -- processes SB messages
|   |       |-- Services/
|   |       |   |-- IExhibitorPublishOrchestrator.cs
|   |       |   +-- ExhibitorPublishOrchestrator.cs     # Calls module PublicApis, transforms, sends
|   |       |-- Messages/
|   |       |   +-- PublishExhibitorMessage.cs
|   |       +-- Models/
|   |           +-- ExternalExhibitorPayload.cs         # Target schema for external system
|   |
|   +-- Modules/
|       |-- Profiles/
|       |   |-- Exhibitor.Profiles.Domain/
|       |   |   |-- Exhibitor.Profiles.Domain.csproj
|       |   |   |-- Entities/
|       |   |   |   |-- Profile.cs                     # Inherits PublishableEntity<ProfileContent>
|       |   |   |   +-- ProfileContent.cs              # Draft/published content model
|       |   |   |-- ValueObjects/
|       |   |   |   |-- ShowroomContact.cs
|       |   |   |   |-- CompanyContact.cs
|       |   |   |   |-- SocialMediaLinks.cs
|       |   |   |   +-- ShowroomPreferences.cs
|       |   |   +-- Enums/
|       |   |
|       |   |-- Exhibitor.Profiles.Features/
|       |   |   |-- Exhibitor.Profiles.Features.csproj
|       |   |   |-- DependencyInjection.cs             # AddProfilesModule()
|       |   |   |-- Services/
|       |   |   |   |-- IProfileService.cs             # Internal: CRUD + publish + discard
|       |   |   |   |-- ProfileService.cs              # Business logic implementation
|       |   |   |   +-- ProfileModuleApi.cs            # Implements IProfileModuleApi
|       |   |   +-- Features/
|       |   |       |-- CreateProfile/                 # Endpoint, request, response, validator, mapping
|       |   |       |-- GetProfile/
|       |   |       |-- UpdateProfile/
|       |   |       |-- DeleteProfile/
|       |   |       |-- ListProfiles/
|       |   |       +-- DiscardDraft/
|       |   |           +-- DiscardDraftEndpoint.cs    # Synchronous -- module-internal
|       |   |
|       |   |-- Exhibitor.Profiles.Infrastructure/
|       |   |   |-- Exhibitor.Profiles.Infrastructure.csproj
|       |   |   |-- DependencyInjection.cs             # AddProfilesInfrastructure()
|       |   |   |-- Interfaces/
|       |   |   |   +-- IProfileRepository.cs
|       |   |   |-- Repositories/
|       |   |   |   +-- ProfileRepository.cs           # Cosmos DB repository
|       |   |   +-- Documents/
|       |   |       +-- ProfileDocument.cs
|       |   |
|       |   +-- Exhibitor.Profiles.PublicApi/
|       |       |-- Exhibitor.Profiles.PublicApi.csproj
|       |       |-- IProfileModuleApi.cs               # PublishAsync, GetPublishedAsync
|       |       +-- Contracts/
|       |           +-- PublishedProfile.cs            # Published snapshot DTOs
|       |
|       +-- Common/
|           |-- Exhibitor.Common.Application/
|           |   |-- Exhibitor.Common.Application.csproj
|           |   |-- Models/
|           |   |   |-- BaseEntity.cs                  # Id, audit fields, soft delete
|           |   |   +-- PublishableEntity.cs           # Abstract: Draft<T>, Published<T>
|           |   +-- Interfaces/
|           |       +-- IPublishableService.cs         # PublishAsync, DiscardDraftAsync
|           |
|           |-- Exhibitor.Common.Cosmos/
|           |   |-- Exhibitor.Common.Cosmos.csproj
|           |   |-- Configuration/CosmosDbConfig.cs
|           |   |-- Documents/
|           |   |   |-- CosmosDbDocument.cs
|           |   |   +-- PublishableDocument.cs
|           |   |-- Extensions/CosmosDbServiceExtensions.cs
|           |   |-- HealthChecks/CosmosDbHealthCheck.cs
|           |   +-- Repositories/CosmosRepositoryBase.cs
|           |
|           +-- Exhibitor.Common.Cosmos.Testing/
|               |-- Exhibitor.Common.Cosmos.Testing.csproj
|               |-- ContainerDefinition.cs
|               |-- CosmosDbFixture.cs
|               +-- CosmosDbFixtureOptions.cs
|
|-- tests/
|   +-- Modules/
|       +-- Profiles/
|           |-- Exhibitor.Profiles.Tests.Unit/
|           |   +-- Exhibitor.Profiles.Tests.Unit.csproj
|           +-- Exhibitor.Profiles.Tests.Integration/
|               +-- Exhibitor.Profiles.Tests.Integration.csproj
|
|-- docs/
|   +-- planning/
|       |-- 00-overview.md
|       +-- ...
|
+-- ExhibitorPlatform.sln
```

### Project References

```
ExhibitorPlatform.WebApi
  +-- Exhibitor.Integration
  +-- Exhibitor.Profiles.Features
  +-- Exhibitor.Profiles.Infrastructure
  +-- Exhibitor.Common.*

Exhibitor.Integration
  +-- Exhibitor.Profiles.PublicApi             # IProfileModuleApi
  +-- Exhibitor.Brands.PublicApi               # IBrandModuleApi (Phase 2)
  +-- FastEndpoints                            # for PublishExhibitorEndpoint
  +-- Azure.Messaging.ServiceBus               # for worker + endpoint

Exhibitor.Profiles.Features
  +-- Exhibitor.Profiles.Domain
  +-- Exhibitor.Profiles.Infrastructure        # for repository interfaces
  +-- Exhibitor.Profiles.PublicApi             # implements IProfileModuleApi
  +-- Exhibitor.Common.Application

Exhibitor.Profiles.Infrastructure
  +-- Exhibitor.Profiles.Domain
  +-- Exhibitor.Common.Cosmos

Exhibitor.Profiles.PublicApi
  +-- (no project references -- only Ardalis.Result NuGet)

Exhibitor.Profiles.Domain
  +-- Exhibitor.Common.Application             # for BaseEntity, PublishableEntity
```

### How Requests Flow

No CQRS. No mediator. Endpoints are thin shells that call service classes.

**HTTP (CRUD) -- endpoint calls service directly:**
```
Client -> FastEndpoints Endpoint -> IProfileService -> IProfileRepository -> Cosmos DB
```

**Publish (async) -- Integration project orchestrates cross-module publish + external sync:**
```
Client -> PublishExhibitorEndpoint -> Service Bus message -> [queue] ->
    ExhibitorPublishWorker (BackgroundService in WebApi process):
        ExhibitorPublishOrchestrator:
            1. IProfileModuleApi.PublishAsync() -> Cosmos DB   [profile module logic]
            2. IBrandModuleApi.PublishAsync() -> Cosmos DB     [brands module logic]
            3. Combine + transform to target schema            [integration logic]
            4. HTTP POST to external system                    [integration logic]
```

**Discard Draft (sync) -- endpoint calls service directly:**
```
Client -> DiscardDraftEndpoint -> IProfileService -> IProfileRepository -> Cosmos DB
```

**Where logic lives:**
- **Module business logic** (publish, CRUD, validation) -> `IProfileService` / `IBrandService` inside each module
- **Cross-module orchestration** (publish both + transform + send) -> `ExhibitorPublishOrchestrator` in Integration project
- **Modules never know about external systems** -- that's the Integration project's job

## Planning Documents

| Doc | Status | Description |
|---|---|---|
| [00-overview.md](00-overview.md) | Done | This document |
| [01-repo-inventory.md](01-repo-inventory.md) | Done | Detailed audit of each source repo |
| [02-module-mapping.md](02-module-mapping.md) | Done | Profiles module structure, endpoint pattern, service layer |
| [03-integration-points.md](03-integration-points.md) | Done | PublicApi interface design & dependency rules |
| [04-cosmos-setup.md](04-cosmos-setup.md) | Done | Cosmos DB setup -- greenfield, no migration needed |
| [05-build-plan.md](05-build-plan.md) | Done | Phased build plan & task breakdown |
| [06-publish-workflow.md](06-publish-workflow.md) | Done | Publish workflow -- Integration project + BackgroundService |
| [07-decisions.md](07-decisions.md) | Done | Architecture decisions, remaining unknowns |
