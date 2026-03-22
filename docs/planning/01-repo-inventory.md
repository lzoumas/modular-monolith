# Repo Inventory

Detailed audit of each source repository's structure, domain models, features, and dependencies.

---

## 1. experience.exhibitor.profile.service

**Repo:** https://github.com/innovationsandmore/experience.exhibitor.profile.service/tree/develop  
**Runtime:** Azure Functions v4 (Isolated Worker), .NET 10  
**Database:** Azure Cosmos DB  
**Result pattern:** Ardalis.Result  
**Validation:** FluentValidation  
**CQRS:** Platform.Shared.Mediator (ICommand / IQuery)

### Project Layout

```
src/
  Exhibitor.Profile.Service.Application/     # Business logic layer
    DependencyInjection.cs
    Features/
      Profile/                                # Vertical slice — commands, queries, handlers
    Interfaces/                               # Repository interfaces
    Models/
      BaseEntity.cs                           # (from Exhibitor.Shared)
      Profile.cs                              # Main domain model
    Services/                                 # Business services
  Exhibitor.Profile.Service.Functions/        # Azure Functions HTTP triggers
  Exhibitor.Profile.Service.Infrastructure/   # Cosmos DB repositories
tests/
  Experience.Exhibitor.Profile.Service.Tests.Unit/
  Experience.Exhibitor.Profile.Service.Tests.Integration/   # Cosmos DB emulator via Docker
```

### Domain Models

- **Profile** — exhibitor profile with company details, media, contacts, attributes, showroom preferences
  - Partition key: `/exhibitorId`
  - Container: `profiles`
  - Supports multiple profiles per exhibitor (channel-based segmentation)
  - Soft delete support
  - Audit fields via `BaseEntity`

### Features (Profile)

| Feature | Type | Description |
|---|---|---|
| Create profile | Command | Create a new exhibitor profile |
| Get profile | Query | Retrieve profile by ID |
| Update profile | Command | Update existing profile |
| Delete profile | Command | Soft delete a profile |
| List profiles | Query | Get profiles for an exhibitor |

### Dependencies

- `Platform.Shared.Mediator` — CQRS command/query handling
- `Platform.Shared.Functions` — HTTP helpers, middleware, parameter attributes
- `Exhibitor.Shared.Application` — BaseEntity
- `Exhibitor.Shared.Cosmos` — CosmosClient, repositories, config, health checks

---

## 2. experience.exhibitor.brands.service

**Repo:** https://github.com/innovationsandmore/experience.exhibitor.brands.service/tree/develop  
**Runtime:** Azure Functions v4 (Isolated Worker), .NET 10  
**Database:** Azure Cosmos DB  
**Result pattern:** Ardalis.Result  
**Validation:** FluentValidation  
**CQRS:** Platform.Shared.Mediator (ICommand / IQuery)

### Project Layout

```
src/
  Experience.Exhibitor.Brands.Service.Application/    # Business logic layer
    DependencyInjection.cs
    Enums/                                            # Brand-related enums
    Features/
      Brand/                                          # Brand-related commands/queries
      FileUploader/                                   # File upload feature
    Interfaces/                                       # Repository interfaces
    Models/
      Brand.cs                                        # Master brand catalog
      ExhibitorBrand.cs                               # Exhibitor-specific brand data
      BrandRequest.cs                                 # Brand request workflow
      Media.cs                                        # Media container
      MediaItem.cs                                    # Individual media asset
      ShowroomAttributes.cs                           # Showroom display config
    Services/                                         # Business services
  Experience.Exhibitor.Brands.Service.Functions/      # Azure Functions HTTP triggers
    DependencyInjection.cs
    Program.cs
    Features/                                         # Function endpoint classes
    appsettings.json / appsettings.{env}.json
  Experience.Exhibitor.Brands.Service.Infrastructure/ # Cosmos DB repositories
tests/
  Experience.Exhibitor.Brands.Service.Tests.Unit/
  Experience.Exhibitor.Brands.Service.Tests.Integration/
```

### Domain Models

- **Brand** — master brand catalog entry with CRM integration (`ExternalBrandId`)
  - Container: `brands`
  - Partition key: `/externalBrandId`
- **ExhibitorBrand** — exhibitor-specific brand data (media, showroom attributes, product categories)
  - Container: `exhibitorBrands`
  - Can be standalone or linked to an approved Brand
- **BrandRequest** — brand request workflow/approval
  - Container: `brandRequests`
- **Media / MediaItem** — media assets for brands
- **ShowroomAttributes** — showroom display configuration

### Features

| Feature | Type | Description |
|---|---|---|
| Brand CRUD | Commands/Queries | Master brand catalog management |
| ExhibitorBrand CRUD | Commands/Queries | Exhibitor-specific brand management |
| BrandRequest workflow | Commands/Queries | Submit, approve, reject brand requests |
| File upload | Command | Upload media assets for brands |

### Dependencies

- `Platform.Shared.Mediator` — CQRS
- `Platform.Shared.Functions` — HTTP helpers, middleware
- `Platform.Shared.FileStorage` — File upload to Azure Blob Storage
- `Exhibitor.Shared.Application` — BaseEntity
- `Exhibitor.Shared.Cosmos` — CosmosClient, repositories

---

## 3. experience.exhibitor.shared.service

**Repo:** https://github.com/innovationsandmore/experience.exhibitor.shared.service  
**Type:** Class library (NuGet packages)  
**Runtime:** .NET 10

### Project Layout

```
src/
  Exhibitor.Shared.Application/
    Models/
      BaseEntity.cs              # Id, CreatedOn/By, ModifiedOn/By, DeletedOn/By, IsDeleted
  Exhibitor.Shared.Cosmos/
    Configuration/
      CosmosDbConfig.cs          # Endpoint, key, database name, retry settings
    Documents/
      CosmosDbDocument.cs        # Base Cosmos document (Id, PartitionKey, audit fields)
    Extensions/
      CosmosDbServiceExtensions.cs   # AddCosmosDbClient() DI extension
    HealthChecks/
      CosmosDbHealthCheck.cs     # IHealthCheck for Cosmos connectivity
    Repositories/
      CosmosRepositoryBase.cs    # Generic CRUD repository base class
  Exhibitor.Shared.Cosmos.Testing/
    ContainerDefinition.cs       # Record for container name + partition key
    CosmosDbFixture.cs           # xUnit fixture with Testcontainers
    CosmosDbFixtureOptions.cs    # Docker image, ports, throughput config
tests/
  Exhibitor.Shared.Application.Tests.Unit/
  Exhibitor.Shared.Cosmos.Tests.Unit/
```

### What This Provides

| Component | Used By | Monolith Mapping |
|---|---|---|
| `BaseEntity` | All domain models | → `Common/Exhibitor.Common.Application` |
| `CosmosDbConfig` | Infrastructure DI | → `Common/Exhibitor.Common.Cosmos` |
| `CosmosDbDocument` | Cosmos repositories | → `Common/Exhibitor.Common.Cosmos` |
| `CosmosRepositoryBase` | All repositories | → `Common/Exhibitor.Common.Cosmos` |
| `CosmosDbHealthCheck` | Host health endpoint | → `Common/Exhibitor.Common.Cosmos` |
| `AddCosmosDbClient()` | Program.cs setup | → `Common/Exhibitor.Common.Cosmos` |
| `CosmosDbFixture` | Integration tests | → `Common/Exhibitor.Common.Cosmos.Testing` |

---

## 4. platform.shared (external — unchanged)

**Repo:** https://github.com/innovationsandmore/platform.shared  
**Type:** Class library (NuGet packages)

### Libraries

| Library | Purpose | Used in Monolith? |
|---|---|---|
| `Platform.Shared.Mediator` | CQRS: ICommand, IQuery, ICommandHandler, IQueryHandler | ✅ Yes — keep as CQRS backbone |
| `Platform.Shared.Functions` | Azure Functions HTTP helpers, middleware, parameter binding | ❌ No — replaced by ASP.NET Core middleware |
| `Platform.Shared.Extensions` | General .NET extensions | ✅ Likely |
| `Platform.Shared.FileStorage` | Azure Blob Storage abstraction | ✅ Yes — brands file upload |
| `Platform.Shared.ServiceBus` | Azure Service Bus helpers | ⚠️ TBD — depends on async messaging needs |
| `Platform.Shared.ManagedIdentityApiClient` | Azure AD managed identity HTTP client | ⚠️ TBD — depends on external API calls |
