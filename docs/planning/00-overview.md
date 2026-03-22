# Consolidation Overview

## Goal

Consolidate three separate Azure Functions microservices into a single **ASP.NET Core Modular Monolith** using vertical-slice architecture, following the patterns established in this repository.

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
| **Host** | 2× Azure Functions apps | 1× ASP.NET Core Web API |
| **Endpoints** | Azure Functions HTTP triggers | Minimal API / Carter / FastEndpoints (see [01-repo-inventory](01-repo-inventory.md)) |
| **CQRS** | `Platform.Shared.Mediator` (ICommand/IQuery) | Keep using `Platform.Shared.Mediator` — already a clean fit |
| **Validation** | FluentValidation + `Platform.Shared.Functions` helpers | FluentValidation (same, just different wiring) |
| **Error handling** | `Ardalis.Result` → HTTP via `Platform.Shared.Functions` | `Ardalis.Result` (keep) or switch to `ErrorOr` for consistency with reference monolith |
| **Database** | Cosmos DB (per-service databases) | Cosmos DB (shared or per-module containers — see [04-data-migration](04-data-migration.md)) |
| **Shared code** | `Exhibitor.Shared.*` NuGet/project refs | Absorbed into a `Common/` project in the monolith |
| **Cross-module calls** | HTTP calls between Function apps | In-process via `PublicApi` interfaces (no network hop) |

## Target Solution Structure (draft)

```
ExhibitorPlatform.Host/                    # ASP.NET Core host — Program.cs, DI, config
Common/
  Exhibitor.Common.Application/            # Absorbed from exhibitor.shared (BaseEntity, etc.)
  Exhibitor.Common.Cosmos/                 # Absorbed from exhibitor.shared (CosmosClient, repos, health)
  Exhibitor.Common.Cosmos.Testing/         # Absorbed from exhibitor.shared (test fixtures)
Profiles/
  Exhibitor.Profiles.Domain/              # Profile entity, value objects
  Exhibitor.Profiles.Features/            # Vertical slices (CreateProfile, GetProfile, etc.)
  Exhibitor.Profiles.Infrastructure/      # Cosmos DB repository, DI
  Exhibitor.Profiles.PublicApi/           # IProfileModuleApi + contracts
Brands/
  Exhibitor.Brands.Domain/               # Brand, ExhibitorBrand, BrandRequest entities
  Exhibitor.Brands.Features/             # Vertical slices (CreateBrand, GetBrand, UploadFile, etc.)
  Exhibitor.Brands.Infrastructure/       # Cosmos DB repository, DI
  Exhibitor.Brands.PublicApi/            # IBrandModuleApi + contracts
tests/
  Exhibitor.Profiles.Tests.Unit/
  Exhibitor.Profiles.Tests.Integration/
  Exhibitor.Brands.Tests.Unit/
  Exhibitor.Brands.Tests.Integration/
```

## Planning Documents

| Doc | Status | Description |
|---|---|---|
| [00-overview.md](00-overview.md) | ✅ | This document |
| [01-repo-inventory.md](01-repo-inventory.md) | 🔲 | Detailed audit of each source repo |
| [02-module-mapping.md](02-module-mapping.md) | 🔲 | How source code maps to monolith modules |
| [03-integration-points.md](03-integration-points.md) | 🔲 | Cross-module communication & PublicApi design |
| [04-data-migration.md](04-data-migration.md) | 🔲 | Cosmos DB strategy (containers, partitioning) |
| [05-migration-plan.md](05-migration-plan.md) | 🔲 | Phased rollout & task breakdown |
| [06-open-questions.md](06-open-questions.md) | 🔲 | Decisions to make, unknowns |
