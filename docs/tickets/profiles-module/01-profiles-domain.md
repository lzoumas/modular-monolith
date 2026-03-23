---
name: Technical Task
about: Create the Profiles Domain Layer
title: '[Tech] Create Profiles Domain Layer'
labels: technical, profiles-module
assignees: ''
---

## Summary

Create `Exhibitor.Profiles.Domain` project and port domain entities, value objects, and enums from the existing profile service.

## Background / Context

The domain layer contains the core business objects with no framework dependencies. `Profile` inherits from `PublishableEntity<ProfileContent>` (from Common), which provides the draft/publish pattern. Value objects are extracted for complex nested data. See [02-module-mapping.md](../../planning/02-module-mapping.md).

**Source:** [experience.exhibitor.profile.service](https://github.com/innovationsandmore/experience.exhibitor.profile.service) `develop` branch -- `Application/Models/` folder.

## Scope

### In Scope
- Create the project with a reference to `Exhibitor.Common.Application`
- Port `Profile` entity and `ProfileContent` model
- Extract value objects (contacts, social media, showroom preferences)
- Port any profile-specific enums

### Out of Scope
- Repository interfaces (see 02-profiles-infrastructure)
- Service classes (see 04-profiles-service-layer)
- Cosmos DB document models (see 02-profiles-infrastructure)

## Implementation Tasks

- [ ] Create `Modules/Profiles/Exhibitor.Profiles.Domain/Exhibitor.Profiles.Domain.csproj`:
  - Target `net9.0`
  - Reference `Exhibitor.Common.Application`
  - No NuGet packages (pure domain -- no framework dependencies)
- [ ] Port `Entities/Profile.cs`:
  - Inherits `PublishableEntity<ProfileContent>`
  - Properties: `ExhibitorId`, `ExhibitorName`, plus audit fields from `BaseEntity`
- [ ] Port `Entities/ProfileContent.cs`:
  - The content model that gets drafted and published
  - Contains showroom contact, company contact, social media, showroom preferences, description, etc.
- [ ] Create value objects in `ValueObjects/`:
  - [ ] `ShowroomContact.cs` -- first name, last name, email, phone
  - [ ] `CompanyContact.cs` -- website, address, phone, email
  - [ ] `SocialMediaLinks.cs` -- LinkedIn, Instagram, Facebook, Twitter/X, etc.
  - [ ] `ShowroomPreferences.cs` -- any showroom-specific settings
- [ ] Port any enums to `Enums/` folder
- [ ] Update namespaces to `Exhibitor.Profiles.Domain.*`
- [ ] Add project to solution file under `Modules > Profiles` solution folder

## Acceptance Criteria

- [ ] Project builds with no errors
- [ ] `Profile` inherits from `PublishableEntity<ProfileContent>`
- [ ] Value objects are separate classes, not inline properties
- [ ] No framework dependencies (no NuGet packages other than Common reference)
- [ ] Namespace follows `Exhibitor.Profiles.Domain.*` convention

## Files to Modify

| File | Change Type |
|------|-------------|
| `Modules/Profiles/Exhibitor.Profiles.Domain/Exhibitor.Profiles.Domain.csproj` | Add |
| `Modules/Profiles/Exhibitor.Profiles.Domain/Entities/Profile.cs` | Add |
| `Modules/Profiles/Exhibitor.Profiles.Domain/Entities/ProfileContent.cs` | Add |
| `Modules/Profiles/Exhibitor.Profiles.Domain/ValueObjects/*.cs` | Add |
| `Modules/Profiles/Exhibitor.Profiles.Domain/Enums/` | Add (if any) |
| `ExhibitorPlatform.sln` | Update |

## Risks / Considerations

- Check if the source `Profile` model has any dependencies on `Platform.Shared` types. If so, either port those types into Common or replace with standard .NET types.
- Value object extraction is a judgment call -- look at the source model and extract nested objects that represent a cohesive concept.

## Verification Steps

1. Run `dotnet build` -- project compiles
2. Verify `Profile` entity has the expected properties
3. Confirm no framework NuGet dependencies in the `.csproj`
