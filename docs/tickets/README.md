# Implementation Tickets

Ticket backlog for the Exhibitor Platform modular monolith migration. Tickets are grouped into three epics, each in its own subfolder.

**Assumptions:**
- The existing profile service ([experience.exhibitor.profile.service](https://github.com/innovationsandmore/experience.exhibitor.profile.service)) has working domain models, services, and repositories.
- Phase 1 ports that code into the modular monolith structure and replaces Azure Function HTTP triggers with FastEndpoints.
- This is greenfield -- no production data or deployments to worry about.

---

## How to Read These Tickets

| Term | Meaning |
|---|---|
| **Epic** | A large body of work (one subfolder) containing multiple stories |
| **Story** | A deliverable chunk of work (feature or technical) -- one markdown file |
| **Sub-task** | A checklist item within a story |
| **Blocked by** | Must be completed before this story can start |

Tickets reference each other as `folder/filename` (e.g. `profiles-module/04-profiles-service-layer`).

---

## project-setup/

**Goal:** Empty but runnable ASP.NET Core host with shared infrastructure wired up. No module code yet -- just the shell.

| # | Ticket | Type | Blocked By | Status |
|---|---|---|---|---|
| 01 | [Remove demo monolith code](project-setup/01-remove-demo-code.md) | Tech | -- | TODO |
| 02 | [Create Host project](project-setup/02-host-project.md) | Tech | 01 | TODO |
| 03 | [Create Common shared libraries](project-setup/03-common-projects.md) | Tech | 01 | TODO |
| 04 | [Local development environment](project-setup/04-local-dev-setup.md) | Tech | 02, 03 | TODO |

**Estimated effort:** 1--2 days

---

## profiles-module/

**Goal:** Migrate the existing profile service CRUD into the modular monolith. Domain, infrastructure, service layer, and all CRUD endpoints working with tests.

| # | Ticket | Type | Blocked By | Status |
|---|---|---|---|---|
| 01 | [Profiles Domain Layer](profiles-module/01-profiles-domain.md) | Tech | project-setup/03 | TODO |
| 02 | [Profiles Infrastructure Layer](profiles-module/02-profiles-infrastructure.md) | Tech | 01 | TODO |
| 03 | [Profiles PublicApi Layer](profiles-module/03-profiles-public-api.md) | Tech | -- | TODO |
| 04 | [Profiles Service Layer and DI](profiles-module/04-profiles-service-layer.md) | Tech | 01, 02, 03 | TODO |
| 05 | [Profile CRUD endpoints](profiles-module/05-profile-crud-endpoints.md) | Feature | 04, project-setup/02 | TODO |
| 06 | [Profile unit tests](profiles-module/06-unit-tests.md) | Tech | 04 | TODO |
| 07 | [Profile integration tests](profiles-module/07-integration-tests.md) | Tech | 05, project-setup/04 | TODO |

**Estimated effort:** 3--5 days

---

## publish-workflow/

**Goal:** Build the async publish architecture -- Azure Functions project, publish endpoint (Service Bus), Function trigger, discard draft, and end-to-end verification.

| # | Ticket | Type | Blocked By | Status |
|---|---|---|---|---|
| 01 | [Create Functions project](publish-workflow/01-functions-project.md) | Tech | project-setup/02, profiles-module/04 | TODO |
| 02 | [Publish Profile workflow](publish-workflow/02-publish-profile-workflow.md) | Feature | 01, profiles-module/03 | TODO |
| 03 | [Discard Draft endpoint](publish-workflow/03-discard-draft-endpoint.md) | Feature | profiles-module/04 | TODO |
| 04 | [End-to-end verification](publish-workflow/04-verification.md) | Tech | profiles-module/05, 02, 03 | TODO |

**Estimated effort:** 2--3 days

---

## Dependency Graph

```
project-setup/01 (Remove demo)
  |-- project-setup/02 (Host) ----+-- project-setup/04 (Local dev)
  |                                |
  +-- project-setup/03 (Common) --+
  |
  +-- profiles-module/01 (Domain) --+-- profiles-module/02 (Infra) --+
                                    |                                 |
profiles-module/03 (PublicApi) -----+-- profiles-module/04 (Service) -+
                                                                      |
                    +-- profiles-module/05 (CRUD) ----+-- profiles-module/07 (Integration tests)
                    |                                 |
                    +-- profiles-module/06 (Unit tests)
                    |
                    +-- publish-workflow/01 (Functions) -- publish-workflow/02 (Publish) --+
                    |                                                                      |
                    +-- publish-workflow/03 (Discard) -------------------------------------+
                    |                                                                      |
                    +----------------------------------------------------------------------+-- publish-workflow/04 (Verification)
```

---

## Execution Order

The epics are sequential but stories within each epic can be parallelized where dependencies allow.

**Sprint 1 -- Project Setup:**
01 -> 02 + 03 (parallel) -> 04

**Sprint 2 -- Profiles Module:**
03 + 01 (parallel) -> 02 -> 04 -> 05 + 06 (parallel) -> 07

**Sprint 3 -- Publish Workflow:**
01 + 03 (parallel) -> 02 -> 04

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
- [02-module-mapping.md](../planning/02-module-mapping.md) -- Module structure
- [05-migration-plan.md](../planning/05-migration-plan.md) -- Phased build plan
- [06-background-and-event-driven.md](../planning/06-background-and-event-driven.md) -- Publish workflow
