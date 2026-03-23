---
name: Technical Task
about: Create the Profiles PublicApi Layer
title: '[Tech] Create Profiles PublicApi Layer'
labels: technical, profiles-module
assignees: ''
---

## Summary

Create `Exhibitor.Profiles.PublicApi` project with the `IProfileModuleApi` interface, contract DTOs, and Service Bus message contracts. This is the only way code outside the Profiles module interacts with profile data.

## Background / Context

The PublicApi project defines the cross-boundary contract for the Profiles module. The Azure Functions project uses `IProfileModuleApi` to call publish logic. Future modules (e.g. Brands) will use it for cross-module queries. The project has no project references -- only the `Ardalis.Result` NuGet package. See [03-integration-points.md](../../planning/03-integration-points.md).

## Scope

### In Scope
- `IProfileModuleApi` interface with publish and query methods
- `PublishedProfile` and related contract DTOs
- `PublishProfileMessage` Service Bus message contract
- No project references (only `Ardalis.Result` NuGet)

### Out of Scope
- Implementation of `IProfileModuleApi` (see 04-profiles-service-layer)
- Azure Function that consumes the message (see publish-workflow/)

## Implementation Tasks

- [ ] Create `Modules/Profiles/Exhibitor.Profiles.PublicApi/Exhibitor.Profiles.PublicApi.csproj`:
  - Target `net9.0`
  - Add `Ardalis.Result` NuGet package
  - No project references
- [ ] Create `IProfileModuleApi.cs`:
  ```
  PublishAsync(exhibitorId, profileId, publishedBy, ct) -> Result<PublishedProfile>
  GetPublishedAsync(exhibitorId, profileId, ct) -> PublishedProfile?
  ```
- [ ] Create `Contracts/PublishedProfile.cs`:
  - Lightweight record with published profile data
  - Only the fields external consumers need (not the full domain model)
  - Include nested content contract if needed (e.g. `PublishedProfileContent`)
- [ ] Create `Messages/PublishProfileMessage.cs`:
  - Record with `ExhibitorId`, `ProfileId`, `PublishedBy`
  - Used by the publish endpoint (sender) and Function (receiver)
- [ ] Add project to solution file under `Modules > Profiles`

## Acceptance Criteria

- [ ] Project builds with no errors
- [ ] `IProfileModuleApi` defines `PublishAsync` and `GetPublishedAsync`
- [ ] `PublishProfileMessage` has all fields needed for the publish workflow
- [ ] `PublishedProfile` is a lightweight DTO -- not the full domain entity
- [ ] No project references in `.csproj` -- only `Ardalis.Result`
- [ ] Both the WebApi and Functions projects can reference this project

## Files to Modify

| File | Change Type |
|------|-------------|
| `Modules/Profiles/Exhibitor.Profiles.PublicApi/Exhibitor.Profiles.PublicApi.csproj` | Add |
| `Modules/Profiles/Exhibitor.Profiles.PublicApi/IProfileModuleApi.cs` | Add |
| `Modules/Profiles/Exhibitor.Profiles.PublicApi/Contracts/PublishedProfile.cs` | Add |
| `Modules/Profiles/Exhibitor.Profiles.PublicApi/Messages/PublishProfileMessage.cs` | Add |
| `ExhibitorPlatform.sln` | Update |

## Risks / Considerations

- Keep contracts minimal. The PublicApi should expose only what external consumers need. Adding too many fields creates coupling.
- `PublishProfileMessage` must be serializable to/from JSON for Service Bus transport (`BinaryData.FromObjectAsJson`).

## Verification Steps

1. Run `dotnet build` -- project compiles
2. Verify `IProfileModuleApi` methods return `Result<T>` from Ardalis.Result
3. Confirm the project has zero project references
