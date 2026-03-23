---
name: Technical Task
about: Profile unit test coverage
title: '[Tech] Profile unit tests'
labels: technical, profiles-module, testing
assignees: ''
---

## Summary

Create `Exhibitor.Profiles.Tests.Unit` project with unit tests covering the Profiles service layer, mapping, and validation logic.

## Background / Context

Unit tests validate business logic in isolation without Cosmos DB or Service Bus dependencies. The primary target is `ProfileService` -- the class that contains all CRUD, publish, and discard logic. Validators and mapping extensions should also be tested.

## Scope

### In Scope
- `ProfileService` unit tests (mock `IProfileRepository`)
- Validator unit tests (Create, Update)
- Mapping extension method tests
- `ProfileModuleApi` unit tests (mock `IProfileService`)

### Out of Scope
- Integration tests with Cosmos DB (see 07-integration-tests)
- Endpoint tests (covered by integration tests)
- Azure Function tests

## Implementation Tasks

- [ ] Create `Profiles/Exhibitor.Profiles.Tests.Unit/Exhibitor.Profiles.Tests.Unit.csproj`:
  - Target `net9.0`
  - Reference `Exhibitor.Profiles.Features`
  - Reference `Exhibitor.Profiles.Domain`
  - Add NuGet packages: `xunit`, `xunit.runner.visualstudio`, `Moq` (or `NSubstitute`), `FluentAssertions`, `Bogus`
- [ ] Create test data builders/factories using Bogus:
  - `ProfileFaker` -- generates valid `Profile` entities
  - `CreateProfileRequestFaker` -- generates valid request DTOs
- [ ] Create `ProfileServiceTests.cs`:
  - [ ] `CreateProfileAsync` -- happy path, returns created profile
  - [ ] `CreateProfileAsync` -- duplicate handling (if applicable)
  - [ ] `GetProfileByIdAsync` -- returns profile when found
  - [ ] `GetProfileByIdAsync` -- returns not-found result when missing
  - [ ] `GetProfilesByExhibitorIdAsync` -- returns list for exhibitor
  - [ ] `UpdateProfileAsync` -- happy path, returns updated profile
  - [ ] `UpdateProfileAsync` -- not found returns error
  - [ ] `DeleteProfileAsync` -- soft deletes profile
  - [ ] `PublishAsync` -- copies draft to published, sets timestamp
  - [ ] `PublishAsync` -- fails when no draft exists
  - [ ] `DiscardDraftAsync` -- reverts draft to published state
  - [ ] `DiscardDraftAsync` -- fails when no draft to discard
- [ ] Create `ProfileModuleApiTests.cs`:
  - [ ] `PublishAsync` -- delegates to service, maps result to contract
  - [ ] `GetPublishedAsync` -- delegates to service, maps result
- [ ] Create `CreateProfileValidatorTests.cs`:
  - [ ] Valid request passes validation
  - [ ] Missing required fields fail validation
  - [ ] Field length violations fail validation
- [ ] Create `UpdateProfileValidatorTests.cs`:
  - [ ] Similar coverage as Create validator
- [ ] Create mapping tests:
  - [ ] Request to domain mapping produces correct entity
  - [ ] Domain to response mapping produces correct DTO
  - [ ] Domain to PublicApi contract mapping produces correct DTO
- [ ] Add project to solution file under `Modules > Profiles` solution folder

## Acceptance Criteria

- [ ] All unit tests pass (`dotnet test`)
- [ ] `ProfileService` has test coverage for all public methods
- [ ] Publish and discard draft business rules are tested
- [ ] Validators are tested for both valid and invalid inputs
- [ ] Tests use mocked repositories (no real database)
- [ ] Test data generated with Bogus (no hardcoded test values)

## Files to Modify

| File | Change Type |
|------|-------------|
| `Profiles/Exhibitor.Profiles.Tests.Unit/Exhibitor.Profiles.Tests.Unit.csproj` | Add |
| `Profiles/Exhibitor.Profiles.Tests.Unit/Services/ProfileServiceTests.cs` | Add |
| `Profiles/Exhibitor.Profiles.Tests.Unit/Services/ProfileModuleApiTests.cs` | Add |
| `Profiles/Exhibitor.Profiles.Tests.Unit/Validators/*.cs` | Add |
| `Profiles/Exhibitor.Profiles.Tests.Unit/Mapping/*.cs` | Add |
| `Profiles/Exhibitor.Profiles.Tests.Unit/Fakers/*.cs` | Add |
| `ExhibitorPlatform.sln` | Update |

## Risks / Considerations

- Check which mocking framework the source repo uses (Moq vs NSubstitute) and stay consistent.
- The publish/discard logic is the most critical business logic -- ensure thorough coverage of edge cases.

## Verification Steps

1. Run `dotnet test --project Profiles/Exhibitor.Profiles.Tests.Unit`
2. All tests pass
3. Review test coverage -- all `ProfileService` methods have at least one test
