---
name: Technical Task
about: Create the Profiles Infrastructure Layer
title: '[Tech] Create Profiles Infrastructure Layer'
labels: technical, profiles-module
assignees: ''
---

## Summary

Create `Exhibitor.Profiles.Infrastructure` project with the Cosmos DB repository, document models, and DI registration.

## Background / Context

See [02-module-mapping.md](../../planning/02-module-mapping.md) and [04-cosmos-setup.md](../../planning/04-cosmos-setup.md).

**Source:** [experience.exhibitor.profile.service](https://github.com/innovationsandmore/experience.exhibitor.profile.service) `develop` branch -- `Infrastructure/` folder.

## Scope

### In Scope
- Create the project with references to Domain and Common.Cosmos
- Port the repository interface
- Port the Cosmos repository implementation
- Create the Cosmos document model
- Create `DependencyInjection.cs` with `AddProfilesInfrastructure()`

### Out of Scope
- Service layer (see 04-profiles-service-layer)
- Cosmos client setup (that is in Common and wired in Host)

## Implementation Tasks

- [ ] Create `Modules/Profiles/Exhibitor.Profiles.Infrastructure/Exhibitor.Profiles.Infrastructure.csproj`:
  - Target `net9.0`
  - Reference `Exhibitor.Profiles.Domain`
  - Reference `Exhibitor.Common.Cosmos`
- [ ] Create `Interfaces/IProfileRepository.cs`:
  - Port from `Application/Interfaces/` in source repo
  - Methods: `GetByIdAsync`, `GetByExhibitorIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` (soft delete)
- [ ] Create `Documents/ProfileDocument.cs`:
  - Inherits `PublishableDocument<ProfileContentDocument>` (or equivalent from Common)
  - Maps to/from `Profile` domain entity
  - Includes `ProfileContentDocument` for the nested draft/published content
- [ ] Create `Repositories/ProfileRepository.cs`:
  - Inherits `CosmosRepositoryBase`
  - Implements `IProfileRepository`
  - Container name: `profiles`
  - Partition key: `exhibitorId`
  - Includes mapping between `ProfileDocument` and `Profile` entity
- [ ] Create `DependencyInjection.cs`:
  - `AddProfilesInfrastructure(this IServiceCollection services)` extension method
  - Registers `IProfileRepository` -> `ProfileRepository` (scoped)
- [ ] Add project to solution file under `Modules > Profiles`

## Acceptance Criteria

- [ ] Project builds with no errors
- [ ] `ProfileRepository` extends `CosmosRepositoryBase`
- [ ] `ProfileDocument` extends `PublishableDocument<T>`
- [ ] `AddProfilesInfrastructure()` registers the repository
- [ ] Repository interface has all CRUD + query methods from the source service

## Files to Modify

| File | Change Type |
|------|-------------|
| `Modules/Profiles/Exhibitor.Profiles.Infrastructure/Exhibitor.Profiles.Infrastructure.csproj` | Add |
| `Modules/Profiles/Exhibitor.Profiles.Infrastructure/Interfaces/IProfileRepository.cs` | Add |
| `Modules/Profiles/Exhibitor.Profiles.Infrastructure/Documents/ProfileDocument.cs` | Add |
| `Modules/Profiles/Exhibitor.Profiles.Infrastructure/Repositories/ProfileRepository.cs` | Add |
| `Modules/Profiles/Exhibitor.Profiles.Infrastructure/DependencyInjection.cs` | Add |
| `ExhibitorPlatform.sln` | Update |

## Risks / Considerations

- The source repository may use `CosmosClient` directly or through the shared `CosmosRepositoryBase`. Verify the base class API matches and adapt if needed.
- Document-to-entity mapping can use manual mapping or a static extension method. Keep it simple -- no AutoMapper.

## Verification Steps

1. Run `dotnet build` -- project compiles
2. Verify `AddProfilesInfrastructure()` is callable from a host `Program.cs`
3. Confirm document model maps all profile fields
