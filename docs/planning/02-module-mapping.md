# Module Mapping

How each source repo maps into the modular monolith project structure.

---

## Mapping Summary

| Source | Target Module | Layer Mapping |
|---|---|---|
| `Exhibitor.Profile.Service.Application/Models/` | `Exhibitor.Profiles.Domain` | Entities, value objects |
| `Exhibitor.Profile.Service.Application/Features/` | `Exhibitor.Profiles.Features` | Vertical slices |
| `Exhibitor.Profile.Service.Application/Interfaces/` | `Exhibitor.Profiles.Infrastructure` | Repository interfaces move to infra (or domain) |
| `Exhibitor.Profile.Service.Application/Services/` | `Exhibitor.Profiles.Features` | Absorbed into handlers |
| `Exhibitor.Profile.Service.Functions/` | `Exhibitor.Profiles.Features` | HTTP triggers → endpoint classes |
| `Exhibitor.Profile.Service.Infrastructure/` | `Exhibitor.Profiles.Infrastructure` | Cosmos repositories |
| `Experience.Exhibitor.Brands.Service.Application/Models/` | `Exhibitor.Brands.Domain` | Entities, value objects, enums |
| `Experience.Exhibitor.Brands.Service.Application/Features/` | `Exhibitor.Brands.Features` | Vertical slices |
| `Experience.Exhibitor.Brands.Service.Application/Interfaces/` | `Exhibitor.Brands.Infrastructure` | Repository interfaces |
| `Experience.Exhibitor.Brands.Service.Application/Services/` | `Exhibitor.Brands.Features` | Absorbed into handlers |
| `Experience.Exhibitor.Brands.Service.Functions/` | `Exhibitor.Brands.Features` | HTTP triggers → endpoint classes |
| `Experience.Exhibitor.Brands.Service.Infrastructure/` | `Exhibitor.Brands.Infrastructure` | Cosmos repositories |
| `Exhibitor.Shared.Application/` | `Common/Exhibitor.Common.Application` | BaseEntity, shared models |
| `Exhibitor.Shared.Cosmos/` | `Common/Exhibitor.Common.Cosmos` | Cosmos client, config, repos, health |
| `Exhibitor.Shared.Cosmos.Testing/` | `Common/Exhibitor.Common.Cosmos.Testing` | Test fixtures |

---

## Detailed Module Layout

### Host — `ExhibitorPlatform.Host`

Replaces both Azure Functions `Program.cs` files. Single ASP.NET Core host.

```
ExhibitorPlatform.Host/
  Program.cs                    # DI wiring, middleware, Cosmos setup, health checks
  appsettings.json              # CosmosDb config, logging, etc.
  appsettings.Development.json
  appsettings.{env}.json        # dev, qa, uat, prod
```

**Responsibilities:**
- Register all modules: `AddProfilesModule()`, `AddBrandsModule()`
- Register infrastructure: `AddProfilesInfrastructure()`, `AddBrandsInfrastructure()`
- Register shared Cosmos client: `AddCosmosDbClient()` (from Common)
- Configure middleware (exception handling, auth, Swagger)
- Health check endpoints

### Common — Shared Libraries

Absorbed from `experience.exhibitor.shared.service`. These are project references, not NuGet.

```
Common/
  Exhibitor.Common.Application/
    Models/
      BaseEntity.cs
  Exhibitor.Common.Cosmos/
    Configuration/CosmosDbConfig.cs
    Documents/CosmosDbDocument.cs
    Extensions/CosmosDbServiceExtensions.cs
    HealthChecks/CosmosDbHealthCheck.cs
    Repositories/CosmosRepositoryBase.cs
  Exhibitor.Common.Cosmos.Testing/
    ContainerDefinition.cs
    CosmosDbFixture.cs
    CosmosDbFixtureOptions.cs
```

### Profiles Module

```
Profiles/
  Exhibitor.Profiles.Domain/
    Entities/
      Profile.cs                 # From Application/Models/Profile.cs
    ValueObjects/                # Extract from Profile if needed (e.g. ContactInfo, CompanyDetails)
    Enums/                       # Profile-specific enums

  Exhibitor.Profiles.Features/
    DependencyInjection.cs       # AddProfilesModule()
    Features/
      CreateProfile/
        CreateProfile.cs         # Endpoint + Command + Handler (vertical slice)
        CreateProfile.Validators.cs
        CreateProfile.Mapping.cs
      GetProfile/
        GetProfile.cs
        GetProfile.Mapping.cs
      UpdateProfile/
        ...
      DeleteProfile/
        ...
      ListProfiles/
        ...

  Exhibitor.Profiles.Infrastructure/
    DependencyInjection.cs       # AddProfilesInfrastructure()
    Repositories/
      ProfileRepository.cs      # From Infrastructure project
    Interfaces/
      IProfileRepository.cs     # From Application/Interfaces

  Exhibitor.Profiles.PublicApi/
    IProfileModuleApi.cs         # For cross-module calls from Brands
    Contracts/
      ProfileSummary.cs          # Lightweight DTOs for cross-module use
```

### Brands Module

```
Brands/
  Exhibitor.Brands.Domain/
    Entities/
      Brand.cs
      ExhibitorBrand.cs
      BrandRequest.cs
    ValueObjects/
      Media.cs
      MediaItem.cs
      ShowroomAttributes.cs
    Enums/                       # From Application/Enums

  Exhibitor.Brands.Features/
    DependencyInjection.cs       # AddBrandsModule()
    Features/
      CreateBrand/
        CreateBrand.cs
        CreateBrand.Validators.cs
        CreateBrand.Mapping.cs
      GetBrand/
        ...
      UpdateBrand/
        ...
      DeleteBrand/
        ...
      CreateExhibitorBrand/
        ...
      GetExhibitorBrand/
        ...
      SubmitBrandRequest/
        ...
      UploadBrandMedia/
        ...

  Exhibitor.Brands.Infrastructure/
    DependencyInjection.cs       # AddBrandsInfrastructure()
    Repositories/
      BrandRepository.cs
      ExhibitorBrandRepository.cs
      BrandRequestRepository.cs
    Interfaces/
      IBrandRepository.cs
      IExhibitorBrandRepository.cs
      IBrandRequestRepository.cs

  Exhibitor.Brands.PublicApi/
    IBrandModuleApi.cs
    Contracts/
      BrandSummary.cs
```

---

## Endpoint Pattern Decision

The current microservices use Azure Functions HTTP triggers with `Platform.Shared.Functions` helpers for parameter binding and validation. In the monolith, we need an ASP.NET Core equivalent.

### Recommendation: **Minimal APIs with a thin wrapper**

| Option | Pros | Cons |
|---|---|---|
| **Carter** | Used in reference monolith, `ICarterModule` per feature | Extra dependency, less control |
| **FastEndpoints** | One class per endpoint, built-in validation, Swagger | New library to learn, opinionated |
| **Minimal APIs (raw)** | No extra deps, built into ASP.NET | Boilerplate, no structure enforcement |
| **Minimal APIs + helper** | Best of both — lightweight, custom conventions | Need to build the helper |

**Suggested approach:** Use **Minimal APIs** with a pattern similar to what `Platform.Shared.Functions` already does — parse, validate, dispatch to handler. This keeps the migration straightforward since the handler logic (commands/queries via `Platform.Shared.Mediator`) stays identical. Only the endpoint "shell" changes from Azure Function trigger → Minimal API route.

The vertical slice stays the same shape:
```
Features/CreateProfile/
  CreateProfile.cs              # Endpoint (Minimal API route mapping)
  CreateProfile.Validators.cs   # FluentValidation (unchanged)
  CreateProfile.Mapping.cs      # DTO ↔ Command mapping (unchanged)
```

The handler continues using `ICommandHandler<TCommand, TResult>` from `Platform.Shared.Mediator` — no change needed.
