# Copilot Instructions — Modular Monolith (Vertical Slice Architecture)

## Project Overview

This is a **.NET 9 Modular Monolith** using vertical-slice architecture. Three business modules — **Shipments**, **Carriers**, and **Stocks** — run in a single ASP.NET Core host (`ModularMonolith.Host`) with strict architectural boundaries between them.

**Database:** PostgreSQL (via Docker), accessed through EF Core with Npgsql. Each module owns its own `DbContext` and migrations. Migrations run automatically on startup.

**Key Libraries:**

| Concern | Library |
|---|---|
| Routing / endpoints | Carter (`ICarterModule`) |
| CQRS / mediator | MediatR |
| Validation | FluentValidation |
| Error handling | ErrorOr |
| Logging | Serilog |
| Test data | Bogus |
| API docs | Swashbuckle (Swagger) |

## Solution Structure

```
ModularMonolith.Host/          # ASP.NET Core host — Program.cs, DI wiring, config
Common/
  Modules.Common.Features/     # Shared helpers (e.g. EndpointResultsExtensions)
Carriers/
  Modules.Carriers.Domain/
  Modules.Carriers.Features/
  Modules.Carriers.Infrastructure/
  Modules.Carriers.PublicApi/
Shipments/
  Modules.Shipments.Domain/
  Modules.Shipments.Features/
  Modules.Shipments.Infrastructure/
Stocks/
  Modules.Stocks.Domain/
  Modules.Stocks.Features/
  Modules.Stocks.Infrastructure/
  Modules.Stocks.PublicApi/
```

## Module Layer Conventions

Each module follows this four-project layout:

| Layer | Project | Purpose |
|---|---|---|
| **Domain** | `Modules.<Name>.Domain` | Entities (`Entities/`), value objects (`ValueObjects/`), enums (`Enums/`). No framework dependencies. |
| **Features** | `Modules.<Name>.Features` | Vertical slices — one folder per feature containing endpoint, command/query, handler, validator, and mapping. Also contains `DependencyInjection.cs` for module service registration. |
| **Infrastructure** | `Modules.<Name>.Infrastructure` | EF Core `DbContext`, entity configurations, migrations, and infrastructure DI registration. |
| **PublicApi** | `Modules.<Name>.PublicApi` | Interface (`I<Name>ModuleApi`) and request/response contracts (`Contracts/`) for cross-module communication. |

## Coding Patterns

### Vertical Slice Features

Each feature lives in `Features/<FeatureName>/` and contains:

- **Endpoint** — A class implementing `ICarterModule` that maps a single route and delegates to MediatR after FluentValidation.
- **Command / Query** — An `internal sealed record` implementing `IRequest<ErrorOr<TResponse>>`.
- **Handler** — An `internal sealed class` using primary constructors for DI, implementing `IRequestHandler<TCommand, ErrorOr<TResponse>>`.
- **Validator** — A separate file (`*.Validators.cs`) with a `Validator` class extending `AbstractValidator<TRequest>`.
- **Mapping** — A separate file (`*.Mapping.cs`) with static extension methods to map between request DTOs, commands, and domain entities.

### Error Handling

Use the `ErrorOr` library. Return `ErrorOr<T>` from handlers. In endpoints, convert errors to HTTP problem details via `response.Errors.ToProblem()` (from `Modules.Common.Features`).

### Cross-Module Communication

Modules **never** reference another module's Domain or Infrastructure projects. They communicate only through `*.PublicApi` interfaces:

- `ICarrierModuleApi` — declared in `Modules.Carriers.PublicApi`
- `IStockModuleApi` — declared in `Modules.Stocks.PublicApi`

The host registers concrete implementations via DI in `Program.cs`.

### Domain Entities

- Use `required` properties for mandatory fields.
- Value objects (e.g. `Address`) are separate classes in `ValueObjects/`.
- No framework dependencies in Domain projects.

### Dependency Injection

Each module exposes a `DependencyInjection.cs` with an `Add<Module>Module()` extension method on `IServiceCollection`. Infrastructure projects similarly expose `Add<Module>Infrastructure()`. Both are called in `Program.cs`.

### Naming

- Namespaces match folder structure (e.g. `Modules.Shipments.Features.Features.CreateShipment`).
- Records for DTOs, requests, commands, and queries.
- Classes for handlers, validators, and endpoints.
- Public API contracts use simple records in `Contracts/` folders.

## Build & Run

```bash
# Start PostgreSQL
docker-compose up -d

# Run the application
dotnet run --project ModularMonolith.Host
```

Swagger UI is available at `/swagger` in development.

## Adding EF Migrations

Target the module's Infrastructure project with the Host as startup project:

```bash
dotnet ef migrations add <Name> --project Shipments/Modules.Shipments.Infrastructure --startup-project ModularMonolith.Host
dotnet ef migrations add <Name> --project Carriers/Modules.Carriers.Infrastructure --startup-project ModularMonolith.Host
dotnet ef migrations add <Name> --project Stocks/Modules.Stocks.Infrastructure --startup-project ModularMonolith.Host
```

## Adding a New Feature

1. Create a folder under `<Module>/Modules.<Module>.Features/Features/<FeatureName>/`.
2. Add the endpoint class (`ICarterModule`), command/query record, handler, validator, and mapping files.
3. Follow existing features (e.g. `CreateShipment`, `GetShipmentByNumber`) as templates.

## Adding a New Module

1. Create four projects: `Domain`, `Features`, `Infrastructure`, `PublicApi` under a new top-level folder.
2. Add `DependencyInjection.cs` to Features and Infrastructure projects.
3. Register the module in `ModularMonolith.Host/Program.cs`.
4. Add the projects to the solution file.
