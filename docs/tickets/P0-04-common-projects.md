---
name: Technical Task
about: Create Common shared library projects
title: '[Tech] Create Common shared libraries'
labels: technical, phase-0
assignees: ''
---

## Summary

Create the three Common projects by absorbing code from [experience.exhibitor.shared.service](https://github.com/innovationsandmore/experience.exhibitor.shared.service). These provide shared base classes, Cosmos DB infrastructure, and test fixtures used by all modules.

## Background / Context

The existing `Exhibitor.Shared.*` libraries contain base entities, Cosmos DB client setup, repository base classes, health checks, and test fixtures. These become `Exhibitor.Common.*` project references in the monolith -- no longer separate NuGet packages.

## Scope

### In Scope
- Create `Exhibitor.Common.Application` (base entities, shared interfaces)
- Create `Exhibitor.Common.Cosmos` (Cosmos client, config, repositories, health checks)
- Create `Exhibitor.Common.Cosmos.Testing` (test fixtures with Testcontainers)
- Port code from `experience.exhibitor.shared.service`
- Update namespaces from `Exhibitor.Shared.*` to `Exhibitor.Common.*`

### Out of Scope
- `Platform.Shared.*` libraries (remain as external references)
- Module-specific code

## Implementation Tasks

### Exhibitor.Common.Application
- [ ] Create `Common/Exhibitor.Common.Application/Exhibitor.Common.Application.csproj` (target `net9.0`, no framework dependencies)
- [ ] Port `Models/BaseEntity.cs` -- Id, audit fields (CreatedOn, CreatedBy, ModifiedOn, ModifiedBy), soft delete (IsDeleted, DeletedOn, DeletedBy)
- [ ] Port `Models/PublishableEntity.cs` -- abstract generic base with Draft content, Published content, PublishedOn, PublishedBy
- [ ] Port `Interfaces/IPublishableService.cs` -- `PublishAsync()`, `DiscardDraftAsync()` contract

### Exhibitor.Common.Cosmos
- [ ] Create `Common/Exhibitor.Common.Cosmos/Exhibitor.Common.Cosmos.csproj`:
  - Add `Microsoft.Azure.Cosmos` NuGet
  - Add `Microsoft.Extensions.Diagnostics.HealthChecks` NuGet
  - Reference `Exhibitor.Common.Application`
- [ ] Port `Configuration/CosmosDbConfig.cs` -- database name, endpoint, key, container definitions
- [ ] Port `Documents/CosmosDbDocument.cs` -- base document with id, partition key, type discriminator
- [ ] Port `Documents/PublishableDocument.cs` -- abstract generic base for draft/published Cosmos docs
- [ ] Port `Extensions/CosmosDbServiceExtensions.cs` -- `AddCosmosDbClient()` DI extension method
- [ ] Port `HealthChecks/CosmosDbHealthCheck.cs` -- implements `IHealthCheck`
- [ ] Port `Repositories/CosmosRepositoryBase.cs` -- generic CRUD operations against a Cosmos container

### Exhibitor.Common.Cosmos.Testing
- [ ] Create `Common/Exhibitor.Common.Cosmos.Testing/Exhibitor.Common.Cosmos.Testing.csproj`:
  - Add `Testcontainers` NuGet (or Cosmos emulator equivalent)
  - Reference `Exhibitor.Common.Cosmos`
- [ ] Port `ContainerDefinition.cs` -- container name + partition key definition
- [ ] Port `CosmosDbFixture.cs` -- xUnit fixture that spins up Cosmos emulator in Docker
- [ ] Port `CosmosDbFixtureOptions.cs` -- database name, Docker container name, container list

### Solution Integration
- [ ] Add all three projects to the solution file under a `Modules > Common` solution folder
- [ ] Update namespaces from `Exhibitor.Shared.*` to `Exhibitor.Common.*`

## Acceptance Criteria

- [ ] All three Common projects build successfully
- [ ] Namespaces follow `Exhibitor.Common.*` convention
- [ ] `BaseEntity` and `PublishableEntity` are available for module domain projects
- [ ] `AddCosmosDbClient()` extension method compiles and accepts container definitions
- [ ] `CosmosRepositoryBase` compiles with generic CRUD methods
- [ ] No dependencies on `Platform.Shared.*` in Common projects

## Files to Modify

| File | Change Type |
|------|-------------|
| `Common/Exhibitor.Common.Application/` | Add (entire project) |
| `Common/Exhibitor.Common.Cosmos/` | Add (entire project) |
| `Common/Exhibitor.Common.Cosmos.Testing/` | Add (entire project) |
| `ExhibitorPlatform.sln` | Update |

## Risks / Considerations

- The source code in `experience.exhibitor.shared.service` targets .NET 10. Port to .NET 9 -- should be straightforward, no breaking API changes expected.
- `CosmosRepositoryBase` may reference `Platform.Shared` types. Check and replace with local equivalents if needed.
- `ContainerDefinition` is used both in the production `AddCosmosDbClient()` call and in test fixtures -- keep the definition in `Exhibitor.Common.Cosmos` and reference it from testing.

## Verification Steps

1. Run `dotnet build` -- all three projects compile
2. Confirm no `Exhibitor.Shared.*` namespaces remain
3. Verify `AddCosmosDbClient()` is callable from a host `Program.cs`
