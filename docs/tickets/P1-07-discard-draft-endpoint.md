---
name: Feature Request
about: Discard Draft endpoint
title: '[Feature] Discard Draft endpoint'
labels: enhancement, phase-1, profiles
assignees: ''
---

## Summary

Create the Discard Draft FastEndpoints endpoint. Unlike publish (which is async via Service Bus), discard is synchronous -- the endpoint calls `IProfileService.DiscardDraftAsync()` directly and returns the result.

## Prerequisites

- P1-04 (Service Layer) -- `IProfileService.DiscardDraftAsync()` must be implemented

## Background / Context

Discarding a draft reverts the profile's draft content to the last published state. This is a simple, synchronous operation -- no Service Bus, no external system calls. The endpoint calls the service directly. See [02-module-mapping.md](../planning/02-module-mapping.md).

## Requirements

- [ ] Endpoint discards draft content and reverts to last published state
- [ ] Returns the updated profile after discard
- [ ] Returns 404 if profile not found
- [ ] Returns 400/409 if there is no draft to discard

## Implementation Tasks

- [ ] Create `Features/DiscardDraft/DiscardDraftEndpoint.cs`:
  - Inherits `EndpointWithoutRequest`
  - Route: `Post("/api/profiles/{exhibitorId}/{profileId}/discard")`
  - Inject `IProfileService`
  - Call `IProfileService.DiscardDraftAsync(exhibitorId, profileId, ct)`
  - Return `200 OK` with updated profile, or appropriate error status

## Acceptance Criteria

- [ ] `POST /api/profiles/{exhibitorId}/{profileId}/discard` returns 200 with updated profile
- [ ] Draft content is reverted to the last published state
- [ ] Returns 404 if profile does not exist
- [ ] Returns appropriate error if there is no draft to discard
- [ ] Endpoint visible in Scalar UI at `/swagger`

## Files to Modify

| File | Change Type |
|------|-------------|
| `Profiles/Exhibitor.Profiles.Features/Features/DiscardDraft/DiscardDraftEndpoint.cs` | Add |

## Out of Scope

- Publish workflow (P1-06)
- Any Service Bus interaction (discard is fully synchronous)

## Technical Notes

- This is the simplest endpoint in the module -- one file, no request DTO, no validator.
- The discard logic lives entirely in `ProfileService.DiscardDraftAsync()`. The endpoint just calls it and returns the result.

## Verification Steps

1. Create a profile and update its draft content
2. Call `POST /api/profiles/{exhibitorId}/{profileId}/discard`
3. Verify the draft content is reverted
4. Verify calling discard when there is no draft returns an appropriate error
