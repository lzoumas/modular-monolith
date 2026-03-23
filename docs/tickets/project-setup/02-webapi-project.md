---
name: Technical Task
about: Create the ASP.NET Core Web API host project
title: '[Tech] Create ExhibitorPlatform.WebApi project'
labels: technical, project-setup
assignees: ''
---

## Summary

Create the `ExhibitorPlatform.WebApi` ASP.NET Core Web API project -- the single entry point for all HTTP traffic. Rename/replace the existing `ModularMonolith.Host` project.

## Background / Context

The modular monolith needs a single ASP.NET Core host that registers all module services, configures middleware, and exposes health check endpoints. This replaces the two separate Azure Functions apps. Uses FastEndpoints for routing (not Carter) and Scalar for OpenAPI docs (not Swashbuckle).

## Scope

### In Scope
- Create (or rename) `ExhibitorPlatform.WebApi` project targeting .NET 9
- Add NuGet packages: FastEndpoints, FastEndpoints.Swagger (Scalar), health checks
- Set up `Program.cs` with FastEndpoints, Scalar, exception handling, health checks
- Create `appsettings.json` and `appsettings.Development.json` with placeholder config
- Add placeholder for module DI registration (`AddProfilesModule()`, `AddProfilesInfrastructure()`)
- Update solution file

### Out of Scope
- Actual module registration (depends on profiles-module/04-profiles-service-layer)
- Cosmos DB wiring (depends on project-setup/04-local-dev-setup)

## Implementation Tasks

- [ ] Rename `ModularMonolith.Host` to `ExhibitorPlatform.WebApi` (or create new and delete old)
- [ ] Update `ExhibitorPlatform.WebApi.csproj` -- target `net9.0`, add NuGet packages:
  - `FastEndpoints`
  - `FastEndpoints.Swagger` (includes Scalar UI)
  - `Microsoft.Extensions.Diagnostics.HealthChecks`
  - `Ardalis.Result` (for shared result pattern)
  - `Serilog.AspNetCore` (logging)
  - `Platform.Shared.ManagedIdentity` (authentication via Azure AD / Entra ID)
- [ ] Create `Program.cs`:
  - `builder.Services.AddFastEndpoints()`
  - `builder.Services.SwaggerDocument(...)` for Scalar
  - Health check endpoint at `/health`
  - Exception handling middleware
  - Configure `Platform.Shared.ManagedIdentity` authentication middleware
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
- [ ] `dotnet run --project ExhibitorPlatform.WebApi` starts the application
- [ ] Scalar UI available at `/swagger` in development
- [ ] Health check endpoint at `/health` returns healthy
- [ ] Solution file updated with correct project name
- [ ] Unauthenticated requests to API endpoints return 401

## Files to Modify

| File | Change Type |
|------|-------------|
| `ExhibitorPlatform.WebApi/ExhibitorPlatform.WebApi.csproj` | Add |
| `ExhibitorPlatform.WebApi/Program.cs` | Add |
| `ExhibitorPlatform.WebApi/appsettings.json` | Add |
| `ExhibitorPlatform.WebApi/appsettings.Development.json` | Add |
| `ExhibitorPlatform.sln` | Update |
| `.github/copilot-instructions.md` | Update |

## Risks / Considerations

- FastEndpoints auto-discovers endpoint classes from referenced assemblies. Module Feature projects must be referenced for endpoints to register.
- Scalar replaces Swashbuckle/Swagger UI -- different configuration pattern.
- `Platform.Shared.ManagedIdentity` handles Azure AD / Entra ID token validation. Check the existing microservices for the exact configuration pattern (audience, tenant, etc.).

## Verification Steps

1. Run `dotnet build` from solution root
2. Run the WebApi -- confirm it starts on the expected port
3. Navigate to `/swagger` -- Scalar UI loads
4. Hit `/health` -- returns 200 with `Healthy` status
