---
name: Technical Task
about: End-to-end verification of the Profiles module
title: '[Tech] End-to-end verification'
labels: technical, phase-1, profiles
assignees: ''
---

## Summary

Verify the complete Profiles module works end-to-end: all CRUD endpoints, the publish workflow (endpoint -> Service Bus -> Function -> Cosmos -> external system), discard draft, and all tests passing.

## Background / Context

This is the final checkpoint for Phase 1. All individual stories should be complete. This ticket is about running the full system together and verifying everything integrates correctly. It is also the place to catch any issues that slipped through individual story testing.

## Scope

### In Scope
- Run all endpoints through Scalar UI
- Test the full publish workflow with a real Service Bus queue (dev environment)
- Verify all unit tests pass
- Verify all integration tests pass
- Verify health checks pass
- Review Scalar/OpenAPI documentation for completeness

### Out of Scope
- CI/CD pipeline (Phase 4)
- Performance testing
- Authentication/authorization (open question)

## Implementation Tasks

- [ ] Start Docker Compose (Cosmos emulator)
- [ ] Start the Host project and verify:
  - [ ] `/health` returns healthy
  - [ ] Scalar UI loads at `/swagger`
  - [ ] All profile endpoints are listed in Scalar
- [ ] Test CRUD flow:
  - [ ] Create a profile -> 201
  - [ ] Get the profile -> 200
  - [ ] Update the profile -> 200
  - [ ] List profiles for the exhibitor -> includes the profile
  - [ ] Delete the profile -> 204
  - [ ] Get the deleted profile -> 404
- [ ] Test publish flow:
  - [ ] Create a profile with draft content
  - [ ] POST to publish endpoint -> 202
  - [ ] Verify Service Bus message was sent
  - [ ] Start the Functions project
  - [ ] Verify the Function picks up the message
  - [ ] Verify the profile's `Published` content is populated in Cosmos
  - [ ] Verify the external HTTP call was attempted (mock or dev endpoint)
- [ ] Test discard flow:
  - [ ] Update a published profile (creates a new draft)
  - [ ] POST to discard endpoint -> 200
  - [ ] Verify draft is reverted to published state
- [ ] Run all tests:
  - [ ] `dotnet test` from solution root -- all pass
- [ ] Review and fix any issues found
- [ ] Update planning doc status (05-migration-plan.md -- mark Phase 1 tasks as done)

## Acceptance Criteria

- [ ] All profile CRUD endpoints return correct HTTP status codes and response bodies
- [ ] Publish workflow works end-to-end (endpoint -> queue -> Function -> Cosmos -> external)
- [ ] Discard draft works correctly
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Health check returns healthy with Cosmos DB connected
- [ ] Scalar UI shows all endpoints with correct request/response schemas
- [ ] No console errors or warnings during normal operation

## Risks / Considerations

- The publish workflow requires a Service Bus namespace (dev environment or local emulator). Ensure connection strings are configured.
- The external system HTTP call needs a target -- use a mock server (e.g. MockServer, WireMock) or a test endpoint.
- If any issues are found, create follow-up bug tickets rather than blocking this verification.

## Verification Steps

This ticket IS the verification -- the implementation tasks above are the verification steps.
