---
name: Technical Task
about: Remove the reference monolith demo code
title: '[Tech] Remove reference monolith demo code'
labels: technical, phase-0
assignees: ''
---

## Summary

Remove the Shipments, Carriers, and Stocks demo modules and all associated dependencies. These were pattern references for the modular monolith architecture -- the planning docs now capture those patterns.

## Background / Context

The current repo contains a reference implementation with three demo modules (Shipments, Carriers, Stocks) that use Carter, MediatR, and ErrorOr. The exhibitor platform uses FastEndpoints, service classes, and Ardalis.Result instead. The demo code must be removed before building the real module structure.

## Scope

### In Scope
- Remove all Shipments module projects
- Remove all Carriers module projects
- Remove all Stocks module projects
- Remove `Modules.Common.Features` project
- Remove demo-specific NuGet packages (Carter, MediatR, ErrorOr) from the solution
- Clean up `ModularMonolith.Host/Program.cs` (remove demo module registrations)
- Remove demo projects from the solution file
- Remove or update the existing `docker-compose.yml` (PostgreSQL -> will be replaced by Cosmos emulator later)

### Out of Scope
- Creating the new project structure (that is P0-02 through P0-04)
- Removing planning docs or copilot instructions

## Implementation Tasks

- [ ] Remove `Shipments/` folder and all 3 projects (Domain, Features, Infrastructure)
- [ ] Remove `Carriers/` folder and all 4 projects (Domain, Features, Infrastructure, PublicApi)
- [ ] Remove `Stocks/` folder and all 4 projects (Domain, Features, Infrastructure, PublicApi)
- [ ] Remove `Common/Modules.Common.Features/` project
- [ ] Remove all demo project references from `ModularMonolith.Host.csproj`
- [ ] Clean `ModularMonolith.Host/Program.cs` -- remove `AddShipmentsModule()`, `AddCarriersModule()`, etc.
- [ ] Remove demo project entries from `ExhibitorPlatform.sln` (or current `.sln` file)
- [ ] Remove demo-only NuGet packages if no longer needed at solution level
- [ ] Verify the solution still builds (empty host is fine)

## Acceptance Criteria

- [ ] No Shipments, Carriers, or Stocks project files remain
- [ ] Solution file only contains the Host project (empty shell)
- [ ] `dotnet build` succeeds
- [ ] No broken project references

## Files to Modify

| File | Change Type |
|------|-------------|
| `*.sln` | Update (remove project entries) |
| `ModularMonolith.Host/ModularMonolith.Host.csproj` | Update (remove project refs) |
| `ModularMonolith.Host/Program.cs` | Update (remove demo registrations) |
| `Shipments/` | Remove (entire folder) |
| `Carriers/` | Remove (entire folder) |
| `Stocks/` | Remove (entire folder) |
| `Common/Modules.Common.Features/` | Remove |

## Risks / Considerations

- The `.github/copilot-instructions.md` still references the demo structure. It will be updated in a separate ticket (or as part of P0-02) once the new structure is in place.

## Verification Steps

1. Run `dotnet build` -- should succeed with empty host
2. Confirm no leftover project references in `.sln` or `.csproj` files
3. Confirm `Shipments/`, `Carriers/`, `Stocks/` folders are gone
