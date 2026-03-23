---
name: Technical Task
about: Create the Azure Functions project for background processing
title: '[Tech] Create ExhibitorPlatform.Functions project'
labels: technical, phase-0
assignees: ''
---

## Summary

Create the `ExhibitorPlatform.Functions` Azure Functions (Isolated Worker) project. This handles Service Bus triggers for async workflows like profile publishing.

## Background / Context

The publish workflow is async: the API host drops a message on a Service Bus queue, and an Azure Function picks it up, calls the module's PublicApi, and sends data to an external system. The Functions project shares the same module code as the Host via project references -- no HTTP hop between them. See [06-background-and-event-driven.md](../planning/06-background-and-event-driven.md).

## Scope

### In Scope
- Create `ExhibitorPlatform.Functions` project targeting .NET 9 (Isolated Worker model)
- Set up `Program.cs` with DI registration (same module pattern as Host)
- Add `host.json` with standard Functions configuration
- Add `appsettings.json` / `local.settings.json` with placeholder config
- Create folder structure for function classes (`Functions/Profiles/`)

### Out of Scope
- Actual function implementations (P1-06)
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
  - Add comment placeholders for `AddProfilesModule()`, `AddProfilesInfrastructure()`
  - Register `IHttpClientFactory` with named client for external system
- [ ] Create `host.json` with:
  - Service Bus extension config (max concurrent calls, retry policy)
  - Logging configuration
- [ ] Create `local.settings.json` with:
  - `AzureWebJobsStorage` (use local emulator or `UseDevelopmentStorage=true`)
  - `ServiceBusConnection` (placeholder)
  - `FUNCTIONS_WORKER_RUNTIME: dotnet-isolated`
- [ ] Create empty folder structure: `Functions/Profiles/`
- [ ] Add project to solution file under appropriate solution folder

## Acceptance Criteria

- [ ] `dotnet build` succeeds for the Functions project
- [ ] Functions project starts locally (even with no functions registered)
- [ ] `Program.cs` follows the same DI pattern as the Host
- [ ] Project references are set up for future module registration

## Files to Modify

| File | Change Type |
|------|-------------|
| `ExhibitorPlatform.Functions/ExhibitorPlatform.Functions.csproj` | Add |
| `ExhibitorPlatform.Functions/Program.cs` | Add |
| `ExhibitorPlatform.Functions/host.json` | Add |
| `ExhibitorPlatform.Functions/local.settings.json` | Add |
| `ExhibitorPlatform.sln` | Update |

## Risks / Considerations

- The Functions project and Host project both reference the same module projects. DI registrations (`AddProfilesModule()`, `AddProfilesInfrastructure()`) must be callable from both hosts.
- `local.settings.json` should be in `.gitignore` -- use a `.json.template` or document required settings.

## Verification Steps

1. Run `dotnet build` -- Functions project compiles
2. Start the Functions host locally -- no errors on startup
3. Confirm folder structure matches the planning doc layout
