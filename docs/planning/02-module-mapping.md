# Module Mapping

How the Profile service source repo maps into the modular monolith. Phase 1 focuses on Profiles only -- Brands follows the same pattern later.

---

## Mapping Summary

| Source | Target | Layer Mapping |
|---|---|---|
| `Exhibitor.Profile.Service.Application/Models/` | `Exhibitor.Profiles.Domain` | Entities, value objects |
| `Exhibitor.Profile.Service.Application/Features/` | `Exhibitor.Profiles.Features` | Vertical slices (endpoints) |
| `Exhibitor.Profile.Service.Application/Interfaces/` | `Exhibitor.Profiles.Infrastructure` | Repository interfaces |
| `Exhibitor.Profile.Service.Application/Services/` | `Exhibitor.Profiles.Features/Services/` | Service classes (business logic) |
| `Exhibitor.Profile.Service.Functions/` | `Exhibitor.Profiles.Features` | HTTP triggers -> FastEndpoints classes |
| `Exhibitor.Profile.Service.Infrastructure/` | `Exhibitor.Profiles.Infrastructure` | Cosmos repositories |
| `Exhibitor.Shared.Application/` | `Common/Exhibitor.Common.Application` | BaseEntity, PublishableEntity, shared models |
| `Exhibitor.Shared.Cosmos/` | `Common/Exhibitor.Common.Cosmos` | Cosmos client, config, repos, health |
| `Exhibitor.Shared.Cosmos.Testing/` | `Common/Exhibitor.Common.Cosmos.Testing` | Test fixtures |

---

## Detailed Module Layout

> All module and common projects are grouped under a **`Modules`** solution folder in Visual Studio. On disk, each module has its own folder at the repo root. See [00-overview](00-overview.md) for the full layout.

### Host -- `ExhibitorPlatform.Host`

Single ASP.NET Core host. Replaces the Azure Functions `Program.cs`.

```
ExhibitorPlatform.Host/
  Program.cs                    # DI wiring, middleware, Cosmos setup, health checks
  appsettings.json
  appsettings.Development.json
  appsettings.{env}.json
```

**Responsibilities:**
- Register module: `AddProfilesModule()` + `AddProfilesInfrastructure()`
- Register shared Cosmos client: `AddCosmosDbClient()` (from Common)
- Configure middleware (exception handling, auth, Swagger/Scalar)
- Health check endpoints

### Functions -- `ExhibitorPlatform.Functions`

Azure Functions (Isolated Worker) for the publish workflow. See [06-background-and-event-driven](06-background-and-event-driven.md).

```
ExhibitorPlatform.Functions/
  Program.cs                    # DI wiring -- registers same module as Host
  Functions/
    Profiles/
      PublishProfileFunction.cs # ServiceBusTrigger -> publish -> transform -> send
  host.json
  appsettings.json
```

### Common -- Shared Libraries

Absorbed from `experience.exhibitor.shared.service`. Project references, not NuGet.

**Solution Explorer:** `Modules > Common`
**Disk:** `Common/`

```
Common/
  Exhibitor.Common.Application/
    Models/
      BaseEntity.cs
      PublishableEntity.cs       # Abstract base for draft/publish pattern
    Interfaces/
      IPublishableService.cs     # Shared publish/discard interface
  Exhibitor.Common.Cosmos/
    Configuration/CosmosDbConfig.cs
    Documents/
      CosmosDbDocument.cs
      PublishableDocument.cs     # Abstract base for Cosmos docs with draft/published
    Extensions/CosmosDbServiceExtensions.cs
    HealthChecks/CosmosDbHealthCheck.cs
    Repositories/CosmosRepositoryBase.cs
  Exhibitor.Common.Cosmos.Testing/
    ContainerDefinition.cs
    CosmosDbFixture.cs
    CosmosDbFixtureOptions.cs
```

### Profiles Module

**Solution Explorer:** `Modules > Profiles`
**Disk:** `Profiles/`

```
Profiles/
  Exhibitor.Profiles.Domain/
    Entities/
      Profile.cs                 # Inherits PublishableEntity<ProfileContent>
      ProfileContent.cs          # The content that gets drafted/published
    ValueObjects/
      ShowroomContact.cs
      CompanyContact.cs
      SocialMediaLinks.cs
      ShowroomPreferences.cs
    Enums/

  Exhibitor.Profiles.Features/
    DependencyInjection.cs       # AddProfilesModule()
    Services/
      IProfileService.cs         # Internal service interface (CRUD + publish + discard)
      ProfileService.cs          # Business logic -- called by endpoints AND Functions
      ProfileModuleApi.cs        # Implements IProfileModuleApi, delegates to IProfileService
    Features/
      CreateProfile/
        CreateProfileEndpoint.cs
        CreateProfileRequest.cs
        CreateProfileResponse.cs
        CreateProfileValidator.cs
        CreateProfileMapping.cs
      GetProfile/
        GetProfileEndpoint.cs
        GetProfileResponse.cs
        GetProfileMapping.cs
      UpdateProfile/
        UpdateProfileEndpoint.cs
        UpdateProfileRequest.cs
        UpdateProfileValidator.cs
        UpdateProfileMapping.cs
      DeleteProfile/
        DeleteProfileEndpoint.cs
      ListProfiles/
        ListProfilesEndpoint.cs
        ListProfilesResponse.cs
        ListProfilesMapping.cs
      PublishProfile/
        PublishProfileEndpoint.cs  # Sends SB message, returns 202
      DiscardDraft/
        DiscardDraftEndpoint.cs    # Synchronous -- calls IProfileService directly

  Exhibitor.Profiles.Infrastructure/
    DependencyInjection.cs       # AddProfilesInfrastructure()
    Repositories/
      ProfileRepository.cs
    Interfaces/
      IProfileRepository.cs
    Documents/
      ProfileDocument.cs          # Inherits PublishableDocument<ProfileContentDocument>

  Exhibitor.Profiles.PublicApi/
    IProfileModuleApi.cs         # PublishAsync, GetPublishedAsync -- for Functions
    Contracts/
      PublishedProfile.cs        # Lightweight published snapshot DTOs
    Messages/
      PublishProfileMessage.cs   # Service Bus message contract
```

---

## No CQRS -- Service Layer Instead

The current microservices use `Platform.Shared.Mediator` (`ICommand`/`IQuery`/`ICommandHandler`/`IQueryHandler`). In the monolith, **we drop that entirely**. Here's why and what replaces it:

**Before (microservices):**
```
Azure Function trigger -> Platform.Shared.Mediator dispatch -> ICommandHandler -> service/repo
```

**After (monolith -- HTTP):**
```
FastEndpoints endpoint -> IProfileService -> IProfileRepository -> Cosmos
```

**After (monolith -- Service Bus):**
```
Azure Function trigger -> IProfileModuleApi -> IProfileService -> IProfileRepository -> Cosmos
```

The layers are:

| Layer | Responsibility | Who calls it |
|---|---|---|
| **FastEndpoints endpoint** | HTTP shell -- routing, binding, validation, HTTP status codes | ASP.NET Core pipeline |
| **Azure Function** | Trigger shell -- receives message, orchestrates, calls external systems | Azure Functions runtime |
| **IProfileModuleApi** (PublicApi) | Cross-boundary facade -- exposes module operations to outsiders | Functions, future modules |
| **IProfileService** (internal) | Business logic -- CRUD, publish, discard, validation rules | Endpoints (directly), PublicApi (via adapter) |
| **IProfileRepository** (infra) | Data access -- Cosmos DB reads/writes | ProfileService |

Key rules:
- **Endpoints** inject `IProfileService` directly (they're inside the module)
- **Functions** inject `IProfileModuleApi` (they're outside the module boundary)
- **No mediator, no commands, no queries, no handlers** -- just service methods

## Endpoint Pattern -- FastEndpoints

Each feature folder contains a FastEndpoints endpoint class. The endpoint handles HTTP concerns (routing, binding, validation) and delegates to `IProfileService` for business logic.

```csharp
// Exhibitor.Profiles.Features/Features/CreateProfile/CreateProfileEndpoint.cs

public sealed class CreateProfileEndpoint : Endpoint<CreateProfileRequest, CreateProfileResponse>
{
    private readonly IProfileService _profileService;

    public CreateProfileEndpoint(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public override void Configure()
    {
        Post("/api/profiles/{exhibitorId}");
    }

    public override async Task HandleAsync(CreateProfileRequest req, CancellationToken ct)
    {
        var profile = req.ToProfile();
        var result = await _profileService.CreateProfileAsync(profile, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                AddError(error);

            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await SendCreatedAtAsync<GetProfileEndpoint>(
            new { exhibitorId = result.Value.ExhibitorId, profileId = result.Value.Id },
            result.Value.ToResponse(),
            cancellation: ct);
    }
}
```

**Why FastEndpoints over Minimal APIs?**
- One class per endpoint -- enforces vertical slice structure
- Built-in FluentValidation integration
- Built-in Swagger/Scalar support with endpoint grouping
- Request/response binding without boilerplate
- The endpoint IS the handler -- no separate CQRS command/handler classes needed

**Why business logic lives in `IProfileService`, not the endpoint?**
- The publish workflow needs to call profile logic from the Azure Function
- `IProfileService` is the single source of truth for business rules
- Endpoints are thin HTTP shells; Functions are thin trigger shells; both delegate to the service

---

## Service Layer

The existing `ProfileService` from the microservice maps directly. It keeps its `IProfileService` interface and implementation -- only the caller changes (Function trigger -> FastEndpoints endpoint).

```csharp
// Exhibitor.Profiles.Features/Services/IProfileService.cs
public interface IProfileService : IPublishableService<Profile, ProfileContent>
{
    Task<Result<Profile>> CreateProfileAsync(Profile profile, CancellationToken ct);
    Task<Result> DeleteProfileAsync(string exhibitorId, string id, string deletedBy, CancellationToken ct);
    Task<Result<Profile>> GetProfileByIdAsync(string exhibitorId, string id, CancellationToken ct);
    Task<Result<IReadOnlyList<Profile>>> GetProfilesByExhibitorIdAsync(string exhibitorId, CancellationToken ct);
    Task<Result<Profile>> UpdateProfileAsync(Profile profile, CancellationToken ct);
    // PublishAsync and DiscardDraftAsync inherited from IPublishableService
}
```

`ProfileModuleApi` implements `IProfileModuleApi` (PublicApi) by delegating to `IProfileService` and mapping domain types to lightweight PublicApi contracts:

```csharp
// Exhibitor.Profiles.Features/Services/ProfileModuleApi.cs
internal sealed class ProfileModuleApi : IProfileModuleApi
{
    private readonly IProfileService _profileService;

    public ProfileModuleApi(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task<Result<PublishedProfile>> PublishAsync(
        string exhibitorId, string profileId, string publishedBy, CancellationToken ct)
    {
        var result = await _profileService.PublishAsync(exhibitorId, profileId, publishedBy, ct);
        return result.Map(p => p.ToPublishedContract()); // Map domain -> PublicApi DTO
    }
}
```

---

## Related Documents

- [00-overview.md](00-overview.md) -- Solution structure & architecture
- [06-background-and-event-driven.md](06-background-and-event-driven.md) -- Publish workflow details
- [03-integration-points.md](03-integration-points.md) -- PublicApi & dependency rules
- [Draft/Publish Pattern](https://github.com/innovationsandmore/experience.exhibitor.profile.service/blob/feature/UXP-3481/docs/draft-publish-pattern.md) -- Source domain pattern
