---
name: Technical Task
about: Profile integration test coverage
title: '[Tech] Profile integration tests'
labels: technical, phase-1, profiles, testing
assignees: ''
---

## Summary

Create `Exhibitor.Profiles.Tests.Integration` project with integration tests that exercise the full request pipeline (HTTP -> FastEndpoints -> Service -> Cosmos DB) using the Cosmos DB emulator.

## Background / Context

Integration tests verify the full stack works end-to-end with a real Cosmos DB instance (emulator via Testcontainers). They test endpoint routing, request validation, serialization, repository operations, and response formatting. Uses `CosmosDbFixture` from `Exhibitor.Common.Cosmos.Testing`.

## Scope

### In Scope
- WebApplicationFactory-based test setup targeting `ExhibitorPlatform.Host`
- Cosmos DB emulator via `CosmosDbFixture` (Testcontainers)
- HTTP tests for all CRUD endpoints
- Discard draft endpoint test

### Out of Scope
- Publish workflow end-to-end (requires Service Bus -- covered in P1-10 verification)
- Azure Function integration tests
- Performance testing

## Implementation Tasks

### Test Infrastructure
- [ ] Create `Profiles/Exhibitor.Profiles.Tests.Integration/Exhibitor.Profiles.Tests.Integration.csproj`:
  - Target `net9.0`
  - Reference `ExhibitorPlatform.Host`
  - Reference `Exhibitor.Common.Cosmos.Testing`
  - Add NuGet packages: `xunit`, `xunit.runner.visualstudio`, `Microsoft.AspNetCore.Mvc.Testing`, `FluentAssertions`, `Bogus`
- [ ] Create `ExhibitorPlatformCosmosDbFixture.cs`:
  - Extends `CosmosDbFixture`
  - Configures `ExhibitorPlatformTestDb` database
  - Defines `profiles` container with `/exhibitorId` partition key
- [ ] Create `ExhibitorPlatformWebFactory.cs`:
  - Extends `WebApplicationFactory<Program>`
  - Overrides DI to use the test Cosmos DB fixture
  - Implements `IClassFixture<ExhibitorPlatformCosmosDbFixture>`
- [ ] Create a base test class or collection fixture for shared setup

### Endpoint Tests
- [ ] Create `CreateProfileTests.cs`:
  - [ ] POST valid request -> 201 Created with Location header
  - [ ] POST invalid request (missing fields) -> 400 with validation errors
  - [ ] POST duplicate (if applicable) -> appropriate error
- [ ] Create `GetProfileTests.cs`:
  - [ ] GET existing profile -> 200 with correct data
  - [ ] GET non-existent profile -> 404
- [ ] Create `UpdateProfileTests.cs`:
  - [ ] PUT valid request -> 200 with updated data
  - [ ] PUT non-existent profile -> 404
  - [ ] PUT invalid request -> 400
- [ ] Create `DeleteProfileTests.cs`:
  - [ ] DELETE existing profile -> 204
  - [ ] DELETE non-existent profile -> 404
  - [ ] GET after DELETE -> 404 (soft delete verified)
- [ ] Create `ListProfilesTests.cs`:
  - [ ] GET with profiles -> 200 with list
  - [ ] GET with no profiles -> 200 with empty list
- [ ] Create `DiscardDraftTests.cs`:
  - [ ] POST discard with draft -> 200
  - [ ] POST discard with no draft -> appropriate error
- [ ] Add project to solution file under `Modules > Profiles` solution folder

## Acceptance Criteria

- [ ] All integration tests pass (`dotnet test`)
- [ ] Tests use a real Cosmos DB emulator (Testcontainers)
- [ ] Each CRUD operation is tested for happy path and error cases
- [ ] Tests are isolated -- each test gets a clean state (or uses unique exhibitorIds)
- [ ] Tests run in CI without manual setup (Docker + Testcontainers handles emulator lifecycle)

## Files to Modify

| File | Change Type |
|------|-------------|
| `Profiles/Exhibitor.Profiles.Tests.Integration/Exhibitor.Profiles.Tests.Integration.csproj` | Add |
| `Profiles/Exhibitor.Profiles.Tests.Integration/Fixtures/ExhibitorPlatformCosmosDbFixture.cs` | Add |
| `Profiles/Exhibitor.Profiles.Tests.Integration/Fixtures/ExhibitorPlatformWebFactory.cs` | Add |
| `Profiles/Exhibitor.Profiles.Tests.Integration/Endpoints/*.cs` | Add |
| `ExhibitorPlatform.sln` | Update |

## Risks / Considerations

- Cosmos DB Testcontainers can be slow to start (30--60 seconds). Use `IClassFixture` or `ICollectionFixture` to share the emulator across tests.
- Docker must be available in the CI environment for Testcontainers to work.
- Tests should use unique `exhibitorId` values to avoid cross-test interference, or clean up after themselves.

## Verification Steps

1. Ensure Docker is running
2. Run `dotnet test --project Profiles/Exhibitor.Profiles.Tests.Integration`
3. All tests pass -- emulator starts, tests run, emulator stops
4. Verify no leftover Docker containers after test run
