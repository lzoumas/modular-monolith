# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

**Prerequisites:** Docker (for Postgres), .NET 9 SDK

Start the database:
```bash
docker-compose up -d
```

Run the application:
```bash
dotnet run --project ModularMonolith.Host
```

The app runs Swagger UI at `/swagger` in development. Sample HTTP requests are in `ModularMonolith.Host/_requests/`.

**Database connection** (from `appsettings.json`):
`Server=127.0.0.1;Port=5432;Database=modular_monolith;User Id=admin;Password=admin;`

EF Core migrations run automatically on startup for all three DbContexts.

## Adding EF Migrations

Each module has its own DbContext and migrations folder. To add a migration, target the infrastructure project:

```bash
dotnet ef migrations add <MigrationName> --project Shipments/Modules.Shipments.Infrastructure --startup-project ModularMonolith.Host
dotnet ef migrations add <MigrationName> --project Carriers/Modules.Carriers.Infrastructure --startup-project ModularMonolith.Host
dotnet ef migrations add <MigrationName> --project Stocks/Modules.Stocks.Infrastructure --startup-project ModularMonolith.Host
```

## Architecture

This is a **modular monolith** — three business modules (Shipments, Carriers, Stocks) running in one process with strict architectural boundaries.

### Module structure

Each module follows this project layout:
- `Modules.<Name>.Domain` — entities and value objects, no framework dependencies
- `Modules.<Name>.Features` — vertical slice features (endpoint + MediatR command/query + validator + mapping in one file), Carter for route registration
- `Modules.<Name>.Infrastructure` — EF Core DbContext, migrations, DI registration
- `Modules.<Name>.PublicApi` — interface (`I<Name>ModuleApi`) and request/response contracts for cross-module calls

### Cross-module communication

Modules communicate only through the `*.PublicApi` interfaces — never by referencing another module's Domain or Infrastructure projects. The host wires up the concrete implementations via DI.

Current cross-module calls:
- `Shipments` → `IStockModuleApi` (check stock, decrement stock on shipment creation)
- `Shipments` → `ICarrierModuleApi` (register shipment with carrier)

### Feature slice pattern

Each feature lives in a single file under `Features/<FeatureName>/`:
1. Request record (HTTP input)
2. `ICarterModule` endpoint class — validates, maps to command, dispatches via MediatR
3. MediatR command/query record (`internal`)
4. Handler class (`internal`) — business logic, EF Core, cross-module API calls
5. Validators in a separate `*.Validators.cs` file, mappings in `*.Mapping.cs`

Errors use `ErrorOr<T>` — handlers return `Error.NotFound`, `Error.Conflict`, etc. The endpoint calls `.ToProblem()` (from `Modules.Common.Features`) to convert to HTTP problem responses.

### Seeding

`SeedService` runs on startup (only if the Shipments table is empty) and seeds carriers (DHL, FedEx, UPS, USPS), fake shipments, and product stocks using Bogus.
