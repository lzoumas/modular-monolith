# Implementation Tickets

Ticket index for the Exhibitor Platform modular monolith migration. Organized as Jira-style epics with stories/tasks underneath.

**Assumptions:**
- The existing profile service ([experience.exhibitor.profile.service](https://github.com/innovationsandmore/experience.exhibitor.profile.service)) has working domain models, services, and repositories.
- Phase 1 ports that code into the modular monolith structure and replaces Azure Function HTTP triggers with FastEndpoints.
- This is greenfield -- no production data or deployments to worry about.

---

## How to Read These Tickets

| Term | Meaning |
|---|---|
| **Epic** | A large body of work containing multiple stories |
| **Story** | A deliverable chunk of work (feature or technical) |
| **Sub-task** | A checklist item within a story |
| **Blocked by** | Must be completed before this story can start |

---

## Epic 0: Monolith Shell Setup

**Goal:** Empty but runnable ASP.NET Core host and Azure Functions project with shared infrastructure.

| # | Ticket | Type | Blocked By | Status |
|---|---|---|---|---|
| P0-01 | [Remove demo monolith code](P0-01-remove-demo-code.md) | Tech | -- | TODO |
| P0-02 | [Create Host project](P0-02-host-project.md) | Tech | P0-01 | TODO |
| P0-03 | [Create Functions project](P0-03-functions-project.md) | Tech | P0-02 | TODO |
| P0-04 | [Create Common shared libraries](P0-04-common-projects.md) | Tech | P0-01 | TODO |
| P0-05 | [Local development environment](P0-05-local-dev-setup.md) | Tech | P0-02, P0-04 | TODO |

**Estimated effort:** 1--2 days

---

## Epic 1: Profiles Module

**Goal:** Fully working Profiles module with CRUD endpoints, publish workflow, and tests.

| # | Ticket | Type | Blocked By | Status |
|---|---|---|---|---|
| P1-01 | [Profiles Domain Layer](P1-01-profiles-domain.md) | Tech | P0-04 | TODO |
| P1-02 | [Profiles Infrastructure Layer](P1-02-profiles-infrastructure.md) | Tech | P1-01 | TODO |
| P1-03 | [Profiles PublicApi Layer](P1-03-profiles-public-api.md) | Tech | -- | TODO |
| P1-04 | [Profiles Service Layer and DI](P1-04-profiles-service-layer.md) | Tech | P1-01, P1-02, P1-03 | TODO |
| P1-05 | [Profile CRUD endpoints](P1-05-profile-crud-endpoints.md) | Feature | P1-04, P0-02 | TODO |
| P1-06 | [Publish Profile workflow](P1-06-publish-profile-workflow.md) | Feature | P1-04, P0-03 | TODO |
| P1-07 | [Discard Draft endpoint](P1-07-discard-draft-endpoint.md) | Feature | P1-04 | TODO |
| P1-08 | [Profile unit tests](P1-08-unit-tests.md) | Tech | P1-04 | TODO |
| P1-09 | [Profile integration tests](P1-09-integration-tests.md) | Tech | P1-05, P0-05 | TODO |
| P1-10 | [End-to-end verification](P1-10-verification.md) | Tech | P1-05, P1-06, P1-07 | TODO |

**Estimated effort:** 3--5 days

---

## Dependency Graph

```
P0-01 (Remove demo)
  |-- P0-02 (Host) ----+-- P0-05 (Local dev)
  |                     |
  +-- P0-04 (Common) --+
  |
  +-- P1-01 (Domain) --+-- P1-02 (Infra) --+
                        |                    |
P1-03 (PublicApi) ------+-- P1-04 (Service) -+
                                             |
                        +-- P1-05 (CRUD) ----+-- P1-09 (Integration tests)
                        |                    |
P0-03 (Functions) ------+-- P1-06 (Publish) -+-- P1-10 (Verification)
                        |                    |
                        +-- P1-07 (Discard) -+
                        |
                        +-- P1-08 (Unit tests)
```

---

## Status Legend

- `TODO` -- Not started
- `IN PROGRESS` -- Work has begun
- `IN REVIEW` -- PR open, awaiting review
- `DONE` -- Merged and verified

---

## Related Planning Docs

- [00-overview.md](../planning/00-overview.md) -- Architecture overview
- [02-module-mapping.md](../planning/02-module-mapping.md) -- Module structure
- [05-migration-plan.md](../planning/05-migration-plan.md) -- Phased build plan
- [06-background-and-event-driven.md](../planning/06-background-and-event-driven.md) -- Publish workflow
