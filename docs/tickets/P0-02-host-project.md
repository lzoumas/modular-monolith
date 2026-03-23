---
name: Technical Task
about: Create the ASP.NET Core Web API host project
title: '[Tech] Create ExhibitorPlatform.Host project'
labels: technical, phase-0
assignees: ''
---

## Summary

Create the `ExhibitorPlatform.Host` ASP.NET Core Web API project -- the single entry point for all HTTP traffic. Rename/replace the existing `ModularMonolith.Host` project.

## Background / Context

The modular monolith needs a single ASP.NET Core host that registers all module services, configures middleware, and exposes health check endpoints. This replaces the two separate Azure Functions apps. Uses FastEndpoints for routing (not Carter) and Scalar for OpenAPI docs (not Swashbuckle).

## Scope

### In Scope
- Create (or rename) `ExhibitorPlatform.Host` project targeting .NET 9
- Add NuGet packages: FastEndpoints, FastEndpoints.Swagger (Scalar), health checks
- Set up `Program.cs` with FastEndpoints, Scalar, exception handling, health checks
- Create `appsettings.json` and `appsettings.Development.json` with placeholder config
- Add placeholder for module DI registration (`AddProfilesModule()`, `AddProfilesInfrastructure()`)
- Update solution file

### Out of Scope
- Authentication/authorization (tracked as open question)
- Actual module registration (depends on P1-04)
- Cosmos DB wiring (depends on P0-04)

## Implementation Tasks

- [ ] Rename `ModularMonolith.Host` to `ExhibitorPlatform.Host` (or create new and delete old)
- [ ] Update `ExhibitorPlatform.Host.csproj` -- target `net9.0`, add NuGet packages:
  - `FastEndpoints`
  - `FastEndpoints.Swagger` (includes Scalar UI)
  - `Microsoft.Extensions.Diagnostics.HealthChecks`
  - `Ardalis.Result` (for shared result pattern)
  - `Serilog.AspNetCore` (logging)
- [ ] Create `Program.cs`:
  - `builder.Services.AddFastEndpoints()`
  - `builder.Services.SwaggerDocument(...)` for Scalar
  - Health check endpoint at `/health`
  - Exception handling middleware
  - Comment placeholders for `AddProfilesModule()` and `AddCosmosDbClient()`
  - `app.UseFastEndpoints()` and `app.UseSwaggerGen()`
- [ ] Create `appsettings.json` with sections for:
  - `CosmosDb` (placeholder)
  - `ServiceBus` (placeholder)
  - `Serilog` configuration
- [ ] Create `appsettings.Development.json` with local dev overrides
- [ ] Update solution file to reference new project
- [ ] Update `.github/copilot-instructions.md` to reflect new project structure

## Acceptance Criteria

- [ ] `dotnet build` succeeds
- [ ] `dotnet run --project ExhibitorPlatform.Host` starts the application
- [ ] Scalar UI available at `/swagger` in development
- [ ] Health check endpoint at `/health` returns healthy
- [ ] Solution file updated with correct project name

## Files to Modify

| File | Change Type |
|------|-------------|
| `ExhibitorPlatform.Host/ExhibitorPlatform.Host.csproj` | Add |
| `ExhibitorPlatform.Host/Program.cs` | Add |
| `ExhibitorPlatform.Host/appsettings.json` | Add |
| `ExhibitorPlatform.Host/appsettings.Development.json` | Add |
| `ExhibitorPlatform.sln` | Update |
| `.github/copilot-instructions.md` | Update |

## Risks / Considerations

- FastEndpoints auto-discovers endpoint classes from referenced assemblies. Module Feature projects must be referenced for endpoints to register.
- Scalar replaces Swashbuckle/Swagger UI -- different configuration pattern.

## Verification Steps

1. Run `dotnet build` from solution root
2. Run the host -- confirm it starts on the expected port
3. Navigate to `/swagger` -- Scalar UI loads
4. Hit `/health` -- returns 200 with `Healthy` status
