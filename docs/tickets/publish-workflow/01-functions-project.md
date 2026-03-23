---
name: Technical Task
about: Create the Azure Functions project for background processing
title: '[Tech] Create ExhibitorPlatform.Functions project'
labels: technical, publish-workflow
assignees: ''
---

## Summary

Create the `ExhibitorPlatform.Functions` Azure Functions (Isolated Worker) project. This handles Service Bus triggers for async workflows like profile publishing.

## Background / Context

See [06-publish-workflow.md](../../planning/06-publish-workflow.md).

## Scope

### In Scope
- Create `ExhibitorPlatform.Functions` project targeting .NET 9 (Isolated Worker model)
- Set up `Program.cs` with DI registration (same module pattern as WebApi)
- Add `host.json` with standard Functions configuration
- Add `appsettings.json` / `local.settings.json` with placeholder config
- Create folder structure for function classes (`Functions/Profiles/`)

### Out of Scope
- Actual function implementations (see 02-publish-profile-workflow)
- Service Bus queue/topic creation (infrastructure concern)

## Implementation Tasks

- [ ] Create `ExhibitorPlatform.Functions/ExhibitorPlatform.Functions.csproj`:
  - Target `net9.0`
  - Add NuGet packages:
    - `Microsoft.Azure.Functions.Worker`
    - `Microsoft.Azure.Functions.Worker.Sdk`
    - `Microsoft.Azure.Functions.Worker.Extensions.ServiceBus`
    - `Microsoft.Azure.Functions.Worker.Extensions.Http`
    - `Microsoft.Extensions.Http`
    - `Serilog.Extensions.Logging`
- [ ] Create `Program.cs`:
  - Configure Isolated Worker host
  - Register Serilog
  - Call `AddProfilesModule()`, `AddProfilesInfrastructure()`
  - Register `IHttpClientFactory` with named client for external system
- [ ] Create `host.json` with:
  - Service Bus extension config (max concurrent calls, retry policy)
  - Logging configuration
- [ ] Create `local.settings.json` with:
  - `AzureWebJobsStorage` (use local emulator or `UseDevelopmentStorage=true`)
  - `ServiceBusConnection` (placeholder)
  - `FUNCTIONS_WORKER_RUNTIME: dotnet-isolated`
- [ ] Create empty folder structure: `Functions/Profiles/`
- [ ] Add project references:
  - `Exhibitor.Profiles.Features` (for DI registration)
  - `Exhibitor.Profiles.Infrastructure` (for DI registration)
  - `Exhibitor.Profiles.PublicApi` (for `IProfileModuleApi` used in Functions)
  - `Exhibitor.Common.*`
- [ ] Add project to solution file

## Acceptance Criteria

- [ ] `dotnet build` succeeds for the Functions project
- [ ] Functions project starts locally (even with no functions registered)
- [ ] `Program.cs` follows the same DI pattern as the WebApi
- [ ] Project references are set up for module resolution

## Files to Modify

| File | Change Type |
|------|-------------|
| `ExhibitorPlatform.Functions/ExhibitorPlatform.Functions.csproj` | Add |
| `ExhibitorPlatform.Functions/Program.cs` | Add |
| `ExhibitorPlatform.Functions/host.json` | Add |
| `ExhibitorPlatform.Functions/local.settings.json` | Add |
| `ExhibitorPlatform.sln` | Update |

## Risks / Considerations

- The Functions project and WebApi project both reference the same module projects.
- `local.settings.json` should be in `.gitignore` -- use a `.json.template` or document required settings.

## Verification Steps

1. Run `dotnet build` -- Functions project compiles
2. Start the Functions host locally -- no errors on startup
3. Confirm folder structure matches the planning doc layout
