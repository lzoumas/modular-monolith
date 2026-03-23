---
name: Technical Task
about: Create the Profiles Service Layer and DI registration
title: '[Tech] Create Profiles Service Layer and DI registration'
labels: technical, phase-1, profiles
assignees: ''
---

## Summary

Create `Exhibitor.Profiles.Features` project with the service layer (`IProfileService`, `ProfileService`, `ProfileModuleApi`) and module DI registration. This is the central business logic layer that endpoints and the Azure Function both delegate to.

## Background / Context

The service layer replaces the CQRS command/query handlers from the existing microservice. `ProfileService` contains all business logic (CRUD, publish, discard draft). `ProfileModuleApi` implements `IProfileModuleApi` (from PublicApi) and delegates to `IProfileService` with domain-to-contract mapping. See [02-module-mapping.md](../planning/02-module-mapping.md).

**Source:** [experience.exhibitor.profile.service](https://github.com/innovationsandmore/experience.exhibitor.profile.service) `develop` branch -- `Application/Services/` folder.

## Scope

### In Scope
- Create the Features project with all required project references
- Port `IProfileService` and `ProfileService` from the source service
- Create `ProfileModuleApi` implementing `IProfileModuleApi`
- Create `DependencyInjection.cs` with `AddProfilesModule()`
- Create empty `Features/` folder structure for endpoint tickets

### Out of Scope
- FastEndpoints endpoint classes (P1-05 through P1-07)
- Validators (created with their respective endpoint tickets)

## Prerequisites

- P1-01 (Domain) -- for entity types
- P1-02 (Infrastructure) -- for `IProfileRepository`
- P1-03 (PublicApi) -- for `IProfileModuleApi` interface

## Implementation Tasks

### Project Setup
- [ ] Create `Profiles/Exhibitor.Profiles.Features/Exhibitor.Profiles.Features.csproj`:
  - Target `net9.0`
  - Reference `Exhibitor.Profiles.Domain`
  - Reference `Exhibitor.Profiles.Infrastructure`
  - Reference `Exhibitor.Profiles.PublicApi`
  - Reference `Exhibitor.Common.Application`
  - Add NuGet packages:
    - `FastEndpoints`
    - `FluentValidation` (for validators in endpoint tickets)
    - `Ardalis.Result`

### Service Layer
- [ ] Create `Services/IProfileService.cs`:
  - Extends `IPublishableService<Profile, ProfileContent>` (from Common)
  - Methods: `CreateProfileAsync`, `GetProfileByIdAsync`, `GetProfilesByExhibitorIdAsync`, `UpdateProfileAsync`, `DeleteProfileAsync`
  - `PublishAsync` and `DiscardDraftAsync` inherited from `IPublishableService`
- [ ] Create `Services/ProfileService.cs`:
  - Port business logic from source `ProfileService`
  - Inject `IProfileRepository`
  - All methods return `Result<T>` from Ardalis.Result
  - Publish logic: copy `Draft` to `Published`, set `PublishedOn`/`PublishedBy`
  - Discard logic: clear `Draft` content, revert to last published state
- [ ] Create `Services/ProfileModuleApi.cs`:
  - `internal sealed class` implementing `IProfileModuleApi`
  - Inject `IProfileService`
  - `PublishAsync` -> delegates to `IProfileService.PublishAsync()`, maps domain entity to `PublishedProfile` contract
  - `GetPublishedAsync` -> delegates to service, maps result

### DI Registration
- [ ] Create `DependencyInjection.cs`:
  - `AddProfilesModule(this IServiceCollection services)` extension method
  - Register `IProfileService` -> `ProfileService` (scoped)
  - Register `IProfileModuleApi` -> `ProfileModuleApi` (scoped)
  - Register FluentValidation validators from assembly (for future endpoint validators)

### Folder Structure
- [ ] Create empty feature folders (endpoints added in subsequent tickets):
  - `Features/CreateProfile/`
  - `Features/GetProfile/`
  - `Features/UpdateProfile/`
  - `Features/DeleteProfile/`
  - `Features/ListProfiles/`
  - `Features/PublishProfile/`
  - `Features/DiscardDraft/`

### Host Wiring
- [ ] Update `ExhibitorPlatform.Host/Program.cs`:
  - Add project reference to `Exhibitor.Profiles.Features`
  - Add project reference to `Exhibitor.Profiles.Infrastructure`
  - Call `builder.Services.AddProfilesModule()`
  - Call `builder.Services.AddProfilesInfrastructure()`
- [ ] Update `ExhibitorPlatform.Functions/Program.cs` with same registrations

## Acceptance Criteria

- [ ] Project builds with no errors
- [ ] `IProfileService` has all CRUD + publish + discard methods
- [ ] `ProfileService` compiles with business logic ported from source
- [ ] `ProfileModuleApi` correctly maps domain types to PublicApi contracts
- [ ] `AddProfilesModule()` registers all services
- [ ] Host and Functions both call `AddProfilesModule()` and `AddProfilesInfrastructure()` in `Program.cs`

## Files to Modify

| File | Change Type |
|------|-------------|
| `Profiles/Exhibitor.Profiles.Features/Exhibitor.Profiles.Features.csproj` | Add |
| `Profiles/Exhibitor.Profiles.Features/Services/IProfileService.cs` | Add |
| `Profiles/Exhibitor.Profiles.Features/Services/ProfileService.cs` | Add |
| `Profiles/Exhibitor.Profiles.Features/Services/ProfileModuleApi.cs` | Add |
| `Profiles/Exhibitor.Profiles.Features/DependencyInjection.cs` | Add |
| `ExhibitorPlatform.Host/Program.cs` | Update |
| `ExhibitorPlatform.Host/ExhibitorPlatform.Host.csproj` | Update (add project refs) |
| `ExhibitorPlatform.Functions/Program.cs` | Update |
| `ExhibitorPlatform.Functions/ExhibitorPlatform.Functions.csproj` | Update (add project refs) |
| `ExhibitorPlatform.sln` | Update |

## Risks / Considerations

- The source `ProfileService` may depend on `Platform.Shared.Mediator` abstractions. Strip those out -- the service methods are called directly now, not through a mediator pipeline.
- Verify the `PublishAsync` logic correctly handles the draft-to-published state transition. This is a critical business rule.
- The mapping from domain `Profile` to `PublishedProfile` contract should be a static extension method in a `Mapping` class, not inline in `ProfileModuleApi`.

## Verification Steps

1. Run `dotnet build` -- entire solution compiles
2. Start the Host -- DI container resolves all services without errors
3. Start the Functions project -- same DI resolution works
