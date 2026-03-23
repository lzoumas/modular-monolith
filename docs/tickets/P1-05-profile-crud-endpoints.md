---
name: Feature Request
about: Profile CRUD endpoints using FastEndpoints
title: '[Feature] Profile CRUD endpoints'
labels: enhancement, phase-1, profiles
assignees: ''
---

## Summary

Create the five CRUD FastEndpoints for the Profiles module: Create, Get, Update, Delete, and List. Each endpoint is a thin HTTP shell that delegates to `IProfileService`.

## Prerequisites

- P1-04 (Service Layer) -- `IProfileService` must be registered
- P0-02 (Host) -- FastEndpoints must be configured

## Background / Context

These endpoints replace the Azure Function HTTP triggers from the existing profile service. FastEndpoints provides one-class-per-endpoint with built-in FluentValidation and Swagger support. Endpoints inject `IProfileService` directly (they are inside the module boundary). See [02-module-mapping.md](../planning/02-module-mapping.md) for the endpoint pattern.

## Requirements

- [ ] All five CRUD operations available via REST API
- [ ] Request validation via FluentValidation on Create and Update
- [ ] Proper HTTP status codes (201 Created, 200 OK, 204 No Content, 404 Not Found)
- [ ] Swagger/Scalar documentation for all endpoints
- [ ] Consistent error response format using Ardalis.Result

## Implementation Tasks

### Sub-task 1: Create Profile -- `POST /api/profiles/{exhibitorId}`
- [ ] Create `Features/CreateProfile/CreateProfileRequest.cs` -- request DTO
- [ ] Create `Features/CreateProfile/CreateProfileResponse.cs` -- response DTO
- [ ] Create `Features/CreateProfile/CreateProfileValidator.cs` -- FluentValidation rules (required fields, string lengths)
- [ ] Create `Features/CreateProfile/CreateProfileMapping.cs` -- `ToProfile()` and `ToResponse()` extension methods
- [ ] Create `Features/CreateProfile/CreateProfileEndpoint.cs`:
  - Inherits `Endpoint<CreateProfileRequest, CreateProfileResponse>`
  - Route: `Post("/api/profiles/{exhibitorId}")`
  - Calls `IProfileService.CreateProfileAsync()`
  - Returns `201 Created` with location header (via `SendCreatedAtAsync`)

### Sub-task 2: Get Profile -- `GET /api/profiles/{exhibitorId}/{profileId}`
- [ ] Create `Features/GetProfile/GetProfileResponse.cs` -- response DTO
- [ ] Create `Features/GetProfile/GetProfileMapping.cs` -- `ToResponse()` extension method
- [ ] Create `Features/GetProfile/GetProfileEndpoint.cs`:
  - Inherits `EndpointWithoutRequest<GetProfileResponse>`
  - Route: `Get("/api/profiles/{exhibitorId}/{profileId}")`
  - Calls `IProfileService.GetProfileByIdAsync()`
  - Returns `200 OK` or `404 Not Found`

### Sub-task 3: Update Profile -- `PUT /api/profiles/{exhibitorId}/{profileId}`
- [ ] Create `Features/UpdateProfile/UpdateProfileRequest.cs` -- request DTO
- [ ] Create `Features/UpdateProfile/UpdateProfileValidator.cs` -- FluentValidation rules
- [ ] Create `Features/UpdateProfile/UpdateProfileMapping.cs` -- `ToProfile()` extension method
- [ ] Create `Features/UpdateProfile/UpdateProfileEndpoint.cs`:
  - Inherits `Endpoint<UpdateProfileRequest>`
  - Route: `Put("/api/profiles/{exhibitorId}/{profileId}")`
  - Calls `IProfileService.UpdateProfileAsync()`
  - Returns `200 OK` or `404 Not Found`

### Sub-task 4: Delete Profile -- `DELETE /api/profiles/{exhibitorId}/{profileId}`
- [ ] Create `Features/DeleteProfile/DeleteProfileEndpoint.cs`:
  - Inherits `EndpointWithoutRequest`
  - Route: `Delete("/api/profiles/{exhibitorId}/{profileId}")`
  - Calls `IProfileService.DeleteProfileAsync()`
  - Returns `204 No Content` or `404 Not Found`

### Sub-task 5: List Profiles -- `GET /api/profiles/{exhibitorId}`
- [ ] Create `Features/ListProfiles/ListProfilesResponse.cs` -- response DTO (list wrapper)
- [ ] Create `Features/ListProfiles/ListProfilesMapping.cs` -- `ToResponse()` extension method
- [ ] Create `Features/ListProfiles/ListProfilesEndpoint.cs`:
  - Inherits `EndpointWithoutRequest<ListProfilesResponse>`
  - Route: `Get("/api/profiles/{exhibitorId}")`
  - Calls `IProfileService.GetProfilesByExhibitorIdAsync()`
  - Returns `200 OK` with list (empty list if none found)

## Acceptance Criteria

- [ ] All five endpoints discoverable in Scalar UI at `/swagger`
- [ ] `POST /api/profiles/{exhibitorId}` creates a profile and returns 201 with location header
- [ ] `GET /api/profiles/{exhibitorId}/{profileId}` returns 200 with profile or 404
- [ ] `PUT /api/profiles/{exhibitorId}/{profileId}` updates and returns 200 or 404
- [ ] `DELETE /api/profiles/{exhibitorId}/{profileId}` soft-deletes and returns 204 or 404
- [ ] `GET /api/profiles/{exhibitorId}` returns 200 with list of profiles
- [ ] Validation errors on Create/Update return 400 with field-level error details
- [ ] All endpoints use `Ardalis.Result` for error handling

## Files to Modify

| File | Change Type |
|------|-------------|
| `Profiles/Exhibitor.Profiles.Features/Features/CreateProfile/*.cs` | Add (5 files) |
| `Profiles/Exhibitor.Profiles.Features/Features/GetProfile/*.cs` | Add (3 files) |
| `Profiles/Exhibitor.Profiles.Features/Features/UpdateProfile/*.cs` | Add (4 files) |
| `Profiles/Exhibitor.Profiles.Features/Features/DeleteProfile/*.cs` | Add (1 file) |
| `Profiles/Exhibitor.Profiles.Features/Features/ListProfiles/*.cs` | Add (3 files) |

## Out of Scope

- Publish endpoint (P1-06)
- Discard draft endpoint (P1-07)
- Authentication/authorization (open question -- tracked in 07-open-questions.md)

## Technical Notes

- FastEndpoints auto-discovers endpoint classes from referenced assemblies. The Host project reference to `Exhibitor.Profiles.Features` is sufficient -- no manual registration needed.
- Use `Route<string>("exhibitorId")` and `Route<string>("profileId")` for path parameter binding.
- Mapping extension methods keep endpoint classes thin -- no inline mapping logic.

## Verification Steps

1. Start the Host project
2. Open Scalar UI at `/swagger`
3. Test each endpoint:
   - Create a profile -> 201 with Location header
   - Get the created profile -> 200
   - Update the profile -> 200
   - List profiles for the exhibitor -> 200 with 1 result
   - Delete the profile -> 204
   - Get the deleted profile -> 404
4. Test validation: submit Create with missing required fields -> 400
