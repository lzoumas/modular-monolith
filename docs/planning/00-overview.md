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

## Target Solution Structure (draft)

### Solution Explorer View

The solution uses a **`Modules` solution folder** (virtual grouping in Visual Studio) to organize all module and common projects — matching the reference monolith pattern used by Shipments, Carriers, and Stocks.

```
Solution 'ExhibitorPlatform'
├── ExhibitorPlatform.Host                     # ASP.NET Core host — Program.cs, DI, config
├── ExhibitorPlatform.Functions                # Azure Functions (Isolated Worker) — Service Bus triggers
├── Modules (solution folder)                  # Virtual grouping — not a physical directory
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

> **Phase 2** adds `Brands` as a sibling solution folder under `Modules`, following the identical pattern.

### Disk Layout

On disk, module folders live at the repo root — the `Modules` grouping is solution-level only.

```
repo-root/
  ExhibitorPlatform.Host/
  ExhibitorPlatform.Functions/
  Profiles/
    Exhibitor.Profiles.Domain/
    Exhibitor.Profiles.Features/
    Exhibitor.Profiles.Infrastructure/
    Exhibitor.Profiles.PublicApi/
  Common/
    Exhibitor.Common.Application/
    Exhibitor.Common.Cosmos/
    Exhibitor.Common.Cosmos.Testing/
  tests/
    Exhibitor.Profiles.Tests.Unit/
    Exhibitor.Profiles.Tests.Integration/
  docs/
  ExhibitorPlatform.sln
```

> **Why this separation matters:** Solution folders are a Visual Studio organizational feature (`.sln` metadata). They don't create physical directories. This matches how the reference monolith groups Carriers, Shipments, Stocks, and Common under a `Modules` solution folder while keeping `Carriers/`, `Stocks/`, etc. at the repo root on disk.

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
