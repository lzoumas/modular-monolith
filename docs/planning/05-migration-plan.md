# Migration Plan

Phased rollout for consolidating the three exhibitor repos into the modular monolith.

---

## Phase 0: Prepare the Monolith Shell

**Goal:** Empty but runnable ASP.NET Core host with shared infrastructure wired up.

- [ ] Remove the existing reference monolith demo code (Shipments, Carriers, Stocks modules)
- [ ] Create `ExhibitorPlatform.Host` ASP.NET Core Web API project
- [ ] Add FastEndpoints NuGet package
- [ ] Set up `Program.cs` with FastEndpoints, Scalar (OpenAPI), health checks, exception handling middleware
- [ ] Create `ExhibitorPlatform.Functions` Azure Functions (Isolated Worker) project
- [ ] Set up Functions `Program.cs` with same module DI registration pattern
- [ ] Copy `Exhibitor.Shared.*` into `Common/` as project references:
  - `Exhibitor.Common.Application` (includes `BaseEntity`, `PublishableEntity<T>`, `IPublishableService<T,C>`)
  - `Exhibitor.Common.Cosmos` (includes `CosmosDbDocument`, `PublishableDocument<T>`, `CosmosRepositoryBase`)
  - `Exhibitor.Common.Cosmos.Testing`
- [ ] Wire up `AddCosmosDbClient()` in `Program.cs` with container definitions
- [ ] Add `docker-compose.yml` for Cosmos DB emulator (or keep using local emulator)
- [ ] Add `platform.shared` as a Git submodule or NuGet package reference
- [ ] Update `.github/copilot-instructions.md` for new structure
- [ ] Verify the host starts, health check returns healthy

---

## Phase 1: Profiles Module

**Goal:** Fully working Profiles module with all endpoints migrated.

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
  - [ ] `Services/ProfileService.cs` -- business logic (migrated from `Application/Services/`)
  - [ ] `Services/ProfileModuleApi.cs` -- implements `IProfileModuleApi`, delegates to `IProfileService`
- [ ] Migrate each feature from `Application/Features/Profile/` as a vertical slice:
  - [ ] `CreateProfile/` -- FastEndpoints endpoint, request, response, validator, mapping
  - [ ] `GetProfile/` -- endpoint, response, mapping
  - [ ] `UpdateProfile/` -- endpoint, request, validator, mapping
  - [ ] `DeleteProfile/` -- endpoint
  - [ ] `ListProfiles/` -- endpoint, response, mapping
  - [ ] `PublishProfile/` -- endpoint (sends Service Bus message, returns 202)
  - [ ] `DiscardDraft/` -- endpoint (synchronous, calls `IProfileService` directly)

### 1d. PublicApi Layer
- [ ] Create `Exhibitor.Profiles.PublicApi` project
- [ ] Define `IProfileModuleApi` interface
- [ ] Add contract DTOs in `Contracts/`

### 1e. Tests
- [ ] Migrate unit tests -> `tests/Exhibitor.Profiles.Tests.Unit/`
- [ ] Migrate integration tests -> `tests/Exhibitor.Profiles.Tests.Integration/`
- [ ] Update test fixture to use shared `CosmosDbFixture`

### 1f. Verify
- [ ] All profile endpoints working via Swagger
- [ ] All unit tests passing
- [ ] All integration tests passing

---

## Phase 2: Brands Module

**Goal:** Fully working Brands module with all endpoints migrated.

### 2a. Domain Layer
- [ ] Create `Exhibitor.Brands.Domain` project
- [ ] Move entities: `Brand.cs`, `ExhibitorBrand.cs`, `BrandRequest.cs`
- [ ] Move value objects: `Media.cs`, `MediaItem.cs`, `ShowroomAttributes.cs`
- [ ] Move enums from `Application/Enums/`

### 2b. Infrastructure Layer
- [ ] Create `Exhibitor.Brands.Infrastructure` project
- [ ] Move repository interfaces
- [ ] Move Cosmos repository implementations
- [ ] Wire up `Platform.Shared.FileStorage` for media uploads
- [ ] Create `DependencyInjection.cs` -> `AddBrandsInfrastructure()`

### 2c. Features Layer
- [ ] Create `Exhibitor.Brands.Features` project
- [ ] Create `DependencyInjection.cs` -> `AddBrandsModule()`
- [ ] Migrate each feature as a vertical slice:
  - [ ] Brand CRUD (Create, Get, Update, Delete, List)
  - [ ] ExhibitorBrand CRUD
  - [ ] BrandRequest workflow (Submit, Approve, Reject, Get)
  - [ ] File upload (UploadBrandMedia)
- [ ] Create service layer (`IBrandService`, `BrandService`, `BrandModuleApi`)
- [ ] Add `PublishBrand/` and `DiscardDraft/` features
- [ ] Add `PublishBrandFunction` to `ExhibitorPlatform.Functions`

### 2d. PublicApi Layer
- [ ] Create `Exhibitor.Brands.PublicApi` project
- [ ] Define `IBrandModuleApi` interface
- [ ] Add contract DTOs in `Contracts/`

### 2e. Tests
- [ ] Migrate unit tests -> `tests/Exhibitor.Brands.Tests.Unit/`
- [ ] Migrate integration tests -> `tests/Exhibitor.Brands.Tests.Integration/`

### 2f. Verify
- [ ] All brand endpoints working via Swagger
- [ ] All tests passing

---

## Phase 3: Cross-Module Integration

**Goal:** Wire up any cross-module communication.

- [ ] Identify actual HTTP calls between current services (search source code for `HttpClient` / `ManagedIdentityApiClient` usage)
- [ ] Replace with PublicApi interface calls
- [ ] Register PublicApi implementations in `Program.cs`
- [ ] Test cross-module scenarios end-to-end

---

## Phase 4: CI/CD & Deployment

**Goal:** Production-ready pipeline.

- [ ] Set up GitHub Actions workflow for build + test
- [ ] Configure Azure deployment (App Service / Container App)
- [ ] Set up environment-specific configuration (dev, qa, uat, prod)
- [ ] Update Cosmos DB connection strings for new database
- [ ] Migrate production data (see [04-data-migration](04-data-migration.md))
- [ ] DNS / routing cutover from Function App endpoints to new API
- [ ] Decommission old Function Apps

---

## Estimated Effort

| Phase | Scope | Estimate |
|---|---|---|
| Phase 0 | Shell setup | 1--2 days |
| Phase 1 | Profiles module | 3--5 days |
| Phase 2 | Brands module | 5--7 days (more models, file upload) |
| Phase 3 | Cross-module wiring | 1--2 days |
| Phase 4 | CI/CD & deployment | 2--3 days |
| **Total** | | **~12--19 days** |
