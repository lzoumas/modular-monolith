# Build Plan

Phased build plan for the exhibitor platform modular monolith. This is greenfield -- the existing profile service has not gone past the dev environment, so there are no production migrations or cutovers.

---

## Phase 0: Prepare the Monolith Shell

**Goal:** Empty but runnable ASP.NET Core host with shared infrastructure wired up.

- [ ] Remove the existing reference monolith demo code (Shipments, Carriers, Stocks modules)
- [ ] Create `ExhibitorPlatform.WebApi` ASP.NET Core Web API project
- [ ] Add FastEndpoints NuGet package
- [ ] Set up `Program.cs` with FastEndpoints, Scalar (OpenAPI), health checks, exception handling middleware
- [ ] Create `Integration/Exhibitor.Integration/` project for cross-module workflows
- [ ] Set up `DependencyInjection.cs` with `AddIntegrationWorkflows()`
- [ ] Copy `Exhibitor.Shared.*` into `Modules/Common/` as project references:
  - `Exhibitor.Common.Application` (includes `BaseEntity`, `PublishableEntity<T>`, `IPublishableService<T,C>`)
  - `Exhibitor.Common.Cosmos` (includes `CosmosDbDocument`, `PublishableDocument<T>`, `CosmosRepositoryBase`)
  - `Exhibitor.Common.Cosmos.Testing`
- [ ] Wire up `AddCosmosDbClient()` in `Program.cs` with container definitions
- [ ] Add `docker-compose.yml` for Cosmos DB emulator
- [ ] Add `platform.shared` as a Git submodule or NuGet package reference
- [ ] Update `.github/copilot-instructions.md` for new structure
- [ ] Verify the host starts, health check returns healthy

---

## Phase 1: Profiles Module

**Goal:** Fully working Profiles module with all endpoints and publish workflow.

### 1a. Domain Layer
- [ ] Create `Exhibitor.Profiles.Domain` project
- [ ] Move `Profile.cs` model to `Entities/`
- [ ] Extract value objects if applicable (ContactInfo, CompanyDetails, etc.)
- [ ] Add any profile-specific enums

### 1b. Infrastructure Layer
- [ ] Create `Exhibitor.Profiles.Infrastructure` project
- [ ] Move repository interface(s) from `Application/Interfaces/`
- [ ] Move Cosmos repository implementation(s) from `Infrastructure/`
- [ ] Create `DependencyInjection.cs` -> `AddProfilesInfrastructure()`

### 1c. Features Layer
- [ ] Create `Exhibitor.Profiles.Features` project
- [ ] Create `DependencyInjection.cs` -> `AddProfilesModule()`
- [ ] Create service layer:
  - [ ] `Services/IProfileService.cs` -- internal service interface (CRUD + publish + discard)
  - [ ] `Services/ProfileService.cs` -- business logic (from `Application/Services/`)
  - [ ] `Services/ProfileModuleApi.cs` -- implements `IProfileModuleApi`, delegates to `IProfileService`
- [ ] Build each feature as a vertical slice:
  - [ ] `CreateProfile/` -- FastEndpoints endpoint, request, response, validator, mapping
  - [ ] `GetProfile/` -- endpoint, response, mapping
  - [ ] `UpdateProfile/` -- endpoint, request, validator, mapping
  - [ ] `DeleteProfile/` -- endpoint
  - [ ] `ListProfiles/` -- endpoint, response, mapping
  - [ ] `DiscardDraft/` -- endpoint (synchronous, calls `IProfileService` directly)

### 1d. PublicApi Layer
- [ ] Create `Exhibitor.Profiles.PublicApi` project
- [ ] Define `IProfileModuleApi` interface
- [ ] Add contract DTOs in `Contracts/`

### 1e. Integration -- Publish Workflow
- [ ] Add `PublishExhibitorEndpoint` to `Exhibitor.Integration` (POST /api/exhibitors/{id}/publish -> SB message)
- [ ] Add `ExhibitorPublishWorker` (BackgroundService with ServiceBusProcessor)
- [ ] Add `ExhibitorPublishOrchestrator` (calls IProfileModuleApi, transforms, sends to external)
- [ ] Wire up Service Bus connection and HTTP client in WebApi `Program.cs`

### 1f. Tests
- [ ] Create unit tests in `Modules/Profiles/Exhibitor.Profiles.Tests.Unit/`
- [ ] Create integration tests in `Modules/Profiles/Exhibitor.Profiles.Tests.Integration/`
- [ ] Set up `CosmosDbFixture` for integration tests

### 1g. Verify
- [ ] All profile endpoints working via Scalar UI
- [ ] Publish workflow end-to-end (endpoint -> Service Bus -> BackgroundService worker -> Cosmos)
- [ ] All unit tests passing
- [ ] All integration tests passing

---

## Phase 2: Brands Module

**Goal:** Fully working Brands module following the same pattern as Profiles.

### 2a. Domain Layer
- [ ] Create `Exhibitor.Brands.Domain` project
- [ ] Move entities: `Brand.cs`, `ExhibitorBrand.cs`, `BrandRequest.cs`
- [ ] Move value objects: `Media.cs`, `MediaItem.cs`, `ShowroomAttributes.cs`
- [ ] Move enums from `Application/Enums/`

### 2b. Infrastructure Layer
- [ ] Create `Exhibitor.Brands.Infrastructure` project
- [ ] Move repository interfaces and Cosmos implementations
- [ ] Wire up `Platform.Shared.FileStorage` for media uploads
- [ ] Create `DependencyInjection.cs` -> `AddBrandsInfrastructure()`

### 2c. Features Layer
- [ ] Create `Exhibitor.Brands.Features` project
- [ ] Create `DependencyInjection.cs` -> `AddBrandsModule()`
- [ ] Build each feature as a vertical slice:
  - [ ] Brand CRUD (Create, Get, Update, Delete, List)
  - [ ] ExhibitorBrand CRUD
  - [ ] BrandRequest workflow (Submit, Approve, Reject, Get)
  - [ ] File upload (UploadBrandMedia)
- [ ] Create service layer (`IBrandService`, `BrandService`, `BrandModuleApi`)
- [ ] Add `PublishBrand/` and `DiscardDraft/` features
- [ ] Update `ExhibitorPublishOrchestrator` in Integration to call `IBrandModuleApi.PublishAsync()`

### 2d. PublicApi Layer
- [ ] Create `Exhibitor.Brands.PublicApi` project
- [ ] Define `IBrandModuleApi` interface
- [ ] Add contract DTOs in `Contracts/`

### 2e. Tests
- [ ] Create unit tests in `Brands/Exhibitor.Brands.Tests.Unit/`
- [ ] Create integration tests in `Brands/Exhibitor.Brands.Tests.Integration/`

### 2f. Verify
- [ ] All brand endpoints working via Scalar UI
- [ ] All tests passing

---

## Phase 3: Cross-Module Integration

**Goal:** Wire up any cross-module communication.

- [ ] Identify actual cross-module calls needed (search source code for `HttpClient` / `ManagedIdentityApiClient` usage)
- [ ] Replace with PublicApi interface calls
- [ ] Register PublicApi implementations in `Program.cs`
- [ ] Test cross-module scenarios end-to-end

---

## Phase 4: CI/CD

**Goal:** Automated build and deploy pipeline.

- [ ] Set up GitHub Actions workflow for build + test
- [ ] Configure Azure deployment (App Service or Container App -- TBD)
- [ ] Set up environment-specific configuration (dev, qa, uat, prod)

---

## Estimated Effort

| Phase | Scope | Estimate |
|---|---|---|
| Phase 0 | Shell setup | 1--2 days |
| Phase 1 | Profiles module | 3--5 days |
| Phase 2 | Brands module | 5--7 days (more models, file upload) |
| Phase 3 | Cross-module wiring | 1--2 days |
| Phase 4 | CI/CD | 1--2 days |
| **Total** | | **~11--18 days** |
