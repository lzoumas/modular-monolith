# Consolidation Overview

## Goal

Consolidate three separate Azure Functions microservices into a single **ASP.NET Core Modular Monolith** using vertical-slice architecture, following the patterns established in this repository.

### Phase 1 Scope: Profiles Only

Phase 1 builds the full architecture with the **Profiles module** — API endpoints (FastEndpoints) + publish workflow (Azure Functions). Once the architecture is proven, Brands follows the same pattern in Phase 2.

## Source Repositories

| Repo | Branch | Purpose | Runtime | Database |
|---|---|---|---|---|
| [experience.exhibitor.profile.service](https://github.com/innovationsandmore/experience.exhibitor.profile.service) | `develop` | Exhibitor profile management (CRUD, media, contacts, showroom) | Azure Functions (Isolated Worker) .NET 10 | Cosmos DB |
| [experience.exhibitor.brands.service](https://github.com/innovationsandmore/experience.exhibitor.brands.service) | `develop` | Brand catalog, exhibitor brands, brand requests, file uploads | Azure Functions (Isolated Worker) .NET 10 | Cosmos DB |
| [experience.exhibitor.shared.service](https://github.com/innovationsandmore/experience.exhibitor.shared.service) | `main` | Shared library — BaseEntity, Cosmos DB client/config/repositories, health checks, test fixtures | Class library .NET 10 | — |

## External Dependency (unchanged)

| Repo | Purpose |
|---|---|
| [platform.shared](https://github.com/innovationsandmore/platform.shared) | Cross-cutting: Mediator (ICommand/IQuery CQRS), Azure Functions helpers, file storage, service bus, managed identity API client, extensions |

`platform.shared` stays as-is. The new monolith will reference the libraries it needs (e.g. `Platform.Shared.Mediator`, `Platform.Shared.FileStorage`, `Platform.Shared.ServiceBus`) as project references or NuGet packages.

## Key Architecture Changes

| Concern | Current (microservices) | Target (modular monolith) |
|---|---|---|
| **Host** | 2× Azure Functions apps | 1× ASP.NET Core Web API + 1× Azure Functions (triggers only) |
| **Endpoints** | Azure Functions HTTP triggers | FastEndpoints (one class per endpoint) |
| **Business logic** | `Platform.Shared.Mediator` handlers | Service classes (`IProfileService`) — no mediator needed |
| **Validation** | FluentValidation | FluentValidation (same, built into FastEndpoints) |
| **Error handling** | `Ardalis.Result` | `Ardalis.Result` (unchanged) |
| **Database** | Cosmos DB | Cosmos DB (same containers, same partition keys) |
| **Shared code** | `Exhibitor.Shared.*` NuGet/project refs | Absorbed into `Common/` projects |
| **Event-driven** | Service Bus triggers in Function App | Separate Azure Functions project, shares module code via project refs |
| **Cross-module calls** | HTTP calls between Function apps | In-process via `PublicApi` interfaces (no network hop) |

## Target Solution Structure

### Solution Explorer View

```
Solution 'ExhibitorPlatform'
├── ExhibitorPlatform.Host                     # ASP.NET Core Web API
├── ExhibitorPlatform.Functions                # Azure Functions (Isolated Worker)
├── Modules (solution folder)
│   ├── Profiles (solution folder)
│   │   ├── Exhibitor.Profiles.Domain
│   │   ├── Exhibitor.Profiles.Features
│   │   ├── Exhibitor.Profiles.Infrastructure
│   │   └── Exhibitor.Profiles.PublicApi
│   └── Common (solution folder)
│       ├── Exhibitor.Common.Application
│       ├── Exhibitor.Common.Cosmos
│       └── Exhibitor.Common.Cosmos.Testing
└── Tests (solution folder)
    ├── Exhibitor.Profiles.Tests.Unit
    └── Exhibitor.Profiles.Tests.Integration
```

> **Phase 2** adds `Brands` as a sibling solution folder under `Modules`.

### Full Disk Layout

```
repo-root/
│
├── ExhibitorPlatform.Host/
│   ├── Program.cs                                 # DI wiring, middleware, Cosmos, health checks
│   ├── ExhibitorPlatform.Host.csproj
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── appsettings.{env}.json                     # dev, qa, uat, prod
│
├── ExhibitorPlatform.Functions/
│   ├── Program.cs                                 # DI wiring — registers same modules as Host
│   ├── ExhibitorPlatform.Functions.csproj
│   ├── host.json
│   ├── appsettings.json
│   └── Functions/
│       └── Profiles/
│           └── PublishProfileFunction.cs           # ServiceBusTrigger → publish → transform → send
│
├── Profiles/
│   ├── Exhibitor.Profiles.Domain/
│   │   ├── Exhibitor.Profiles.Domain.csproj
│   │   ├── Entities/
│   │   │   ├── Profile.cs                         # Inherits PublishableEntity<ProfileContent>
│   │   │   └── ProfileContent.cs                  # Draft/published content model
│   │   ├── ValueObjects/
│   │   │   ├── ShowroomContact.cs
│   │   │   ├── CompanyContact.cs
│   │   │   ├── SocialMediaLinks.cs
│   │   │   └── ShowroomPreferences.cs
│   │   └── Enums/
│   │
│   ├── Exhibitor.Profiles.Features/
│   │   ├── Exhibitor.Profiles.Features.csproj
│   │   ├── DependencyInjection.cs                 # AddProfilesModule() — registers services, validators
│   │   ├── Services/
│   │   │   ├── IProfileService.cs                 # Internal: CRUD + publish + discard
│   │   │   ├── ProfileService.cs                  # Business logic implementation
│   │   │   └── ProfileModuleApi.cs                # Implements IProfileModuleApi → delegates to IProfileService
│   │   └── Features/
│   │       ├── CreateProfile/
│   │       │   ├── CreateProfileEndpoint.cs        # FastEndpoints endpoint
│   │       │   ├── CreateProfileRequest.cs
│   │       │   ├── CreateProfileResponse.cs
│   │       │   ├── CreateProfileValidator.cs       # FluentValidation
│   │       │   └── CreateProfileMapping.cs         # Request ↔ Domain mapping
│   │       ├── GetProfile/
│   │       │   ├── GetProfileEndpoint.cs
│   │       │   ├── GetProfileResponse.cs
│   │       │   └── GetProfileMapping.cs
│   │       ├── UpdateProfile/
│   │       │   ├── UpdateProfileEndpoint.cs
│   │       │   ├── UpdateProfileRequest.cs
│   │       │   ├── UpdateProfileValidator.cs
│   │       │   └── UpdateProfileMapping.cs
│   │       ├── DeleteProfile/
│   │       │   └── DeleteProfileEndpoint.cs
│   │       ├── ListProfiles/
│   │       │   ├── ListProfilesEndpoint.cs
│   │       │   ├── ListProfilesResponse.cs
│   │       │   └── ListProfilesMapping.cs
│   │       ├── PublishProfile/
│   │       │   └── PublishProfileEndpoint.cs       # Sends SB message, returns 202
│   │       └── DiscardDraft/
│   │           └── DiscardDraftEndpoint.cs          # Synchronous — calls IProfileService directly
│   │
│   ├── Exhibitor.Profiles.Infrastructure/
│   │   ├── Exhibitor.Profiles.Infrastructure.csproj
│   │   ├── DependencyInjection.cs                 # AddProfilesInfrastructure() — registers repos
│   │   ├── Interfaces/
│   │   │   └── IProfileRepository.cs
│   │   ├── Repositories/
│   │   │   └── ProfileRepository.cs               # Cosmos DB repository
│   │   └── Documents/
│   │       └── ProfileDocument.cs                 # Inherits PublishableDocument<ProfileContentDocument>
│   │
│   └── Exhibitor.Profiles.PublicApi/
│       ├── Exhibitor.Profiles.PublicApi.csproj
│       ├── IProfileModuleApi.cs                   # PublishAsync, GetPublishedAsync
│       ├── Contracts/
│       │   └── PublishedProfile.cs                # Published snapshot DTOs
│       └── Messages/
│           └── PublishProfileMessage.cs           # Service Bus message contract
│
├── Common/
│   ├── Exhibitor.Common.Application/
│   │   ├── Exhibitor.Common.Application.csproj
│   │   ├── Models/
│   │   │   ├── BaseEntity.cs                      # Id, audit fields, soft delete
│   │   │   └── PublishableEntity.cs               # Abstract: Draft<T>, Published<T>, PublishedOn/By
│   │   └── Interfaces/
│   │       └── IPublishableService.cs             # PublishAsync, DiscardDraftAsync
│   │
│   ├── Exhibitor.Common.Cosmos/
│   │   ├── Exhibitor.Common.Cosmos.csproj
│   │   ├── Configuration/
│   │   │   └── CosmosDbConfig.cs
│   │   ├── Documents/
│   │   │   ├── CosmosDbDocument.cs
│   │   │   └── PublishableDocument.cs             # Abstract: Draft<T>, Published<T> for Cosmos docs
│   │   ├── Extensions/
│   │   │   └── CosmosDbServiceExtensions.cs       # AddCosmosDbClient() DI extension
│   │   ├── HealthChecks/
│   │   │   └── CosmosDbHealthCheck.cs
│   │   └── Repositories/
│   │       └── CosmosRepositoryBase.cs
│   │
│   └── Exhibitor.Common.Cosmos.Testing/
│       ├── Exhibitor.Common.Cosmos.Testing.csproj
│       ├── ContainerDefinition.cs
│       ├── CosmosDbFixture.cs
│       └── CosmosDbFixtureOptions.cs
│
├── tests/
│   ├── Exhibitor.Profiles.Tests.Unit/
│   │   └── Exhibitor.Profiles.Tests.Unit.csproj
│   └── Exhibitor.Profiles.Tests.Integration/
│       └── Exhibitor.Profiles.Tests.Integration.csproj
│
├── docs/
│   └── planning/
│       ├── 00-overview.md
│       └── ...
│
└── ExhibitorPlatform.sln
```

### Project References

```
ExhibitorPlatform.Host
  ├── Exhibitor.Profiles.Features
  ├── Exhibitor.Profiles.Infrastructure
  └── Exhibitor.Common.*

ExhibitorPlatform.Functions
  ├── Exhibitor.Profiles.Features              ← for DI registration (AddProfilesModule)
  ├── Exhibitor.Profiles.Infrastructure        ← for DI registration (AddProfilesInfrastructure)
  ├── Exhibitor.Profiles.PublicApi             ← IProfileModuleApi used in Function classes
  └── Exhibitor.Common.*

Exhibitor.Profiles.Features
  ├── Exhibitor.Profiles.Domain
  ├── Exhibitor.Profiles.Infrastructure        ← for repository interfaces
  ├── Exhibitor.Profiles.PublicApi             ← implements IProfileModuleApi
  └── Exhibitor.Common.Application

Exhibitor.Profiles.Infrastructure
  ├── Exhibitor.Profiles.Domain
  └── Exhibitor.Common.Cosmos

Exhibitor.Profiles.PublicApi
  └── (no project references — only Ardalis.Result NuGet)

Exhibitor.Profiles.Domain
  └── Exhibitor.Common.Application             ← for BaseEntity, PublishableEntity
```

### How Requests Flow

**HTTP (CRUD):**
```
Client → FastEndpoints Endpoint → IProfileService → IProfileRepository → Cosmos DB
```

**Publish (async):**
```
Client → PublishProfileEndpoint → Service Bus message → [queue] →
    PublishProfileFunction → IProfileModuleApi → IProfileService →
        IProfileRepository → Cosmos DB
    → Transform → HTTP to external system
```

**Discard Draft (sync):**
```
Client → DiscardDraftEndpoint → IProfileService → IProfileRepository → Cosmos DB
```

## Planning Documents

| Doc | Status | Description |
|---|---|---|
| [00-overview.md](00-overview.md) | ✅ | This document |
| [01-repo-inventory.md](01-repo-inventory.md) | ✅ | Detailed audit of each source repo |
| [02-module-mapping.md](02-module-mapping.md) | ✅ | Profiles module structure, endpoint pattern, service layer |
| [03-integration-points.md](03-integration-points.md) | ✅ | PublicApi interface design & dependency rules |
| [04-data-migration.md](04-data-migration.md) | 🔲 | Cosmos DB strategy (if any migration needed) |
| [05-migration-plan.md](05-migration-plan.md) | 🔲 | Phased rollout & task breakdown |
| [06-background-and-event-driven.md](06-background-and-event-driven.md) | ✅ | Publish workflow — API endpoint + Azure Function |
| [07-open-questions.md](07-open-questions.md) | ✅ | Decisions resolved, remaining unknowns |
