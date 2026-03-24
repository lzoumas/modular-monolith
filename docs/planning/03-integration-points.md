# Integration Points

How the Integration project and future modules interact with the Profiles module.

---

## PublicApi Interface

The `IProfileModuleApi` interface is the **only** way code outside the Profiles module accesses profile data. The Integration project uses it for the publish workflow. Future modules (e.g. Brands) will use it for cross-module queries.

```csharp
// Exhibitor.Profiles.PublicApi/IProfileModuleApi.cs
namespace Exhibitor.Profiles.PublicApi;

public interface IProfileModuleApi
{
    Task<Result<PublishedProfile>> PublishAsync(
        string exhibitorId, string profileId, string publishedBy,
        CancellationToken ct = default);

    Task<PublishedProfile?> GetPublishedAsync(
        string exhibitorId, string profileId,
        CancellationToken ct = default);
}
```

The implementation (`ProfileModuleApi`) lives in `Exhibitor.Profiles.Features/Services/` and delegates to `IProfileService`. See [02-module-mapping](02-module-mapping.md) for details.

---

## Dependency Rules

```
ExhibitorPlatform.WebApi
  |-- Exhibitor.Integration                    <- publish workflow, cross-module orchestration
  |-- Exhibitor.Profiles.Features              <- for DI registration (AddProfilesModule)
  |-- Exhibitor.Profiles.Infrastructure        <- for DI registration (AddProfilesInfrastructure)
  |-- Exhibitor.Common.*
  +-- Platform.Shared.* (external, if needed)

Exhibitor.Integration
  |-- Exhibitor.Profiles.PublicApi             <- uses IProfileModuleApi
  +-- Exhibitor.Brands.PublicApi               <- uses IBrandModuleApi (Phase 2)

Exhibitor.Profiles.Features
  |-- Exhibitor.Profiles.Domain
  |-- Exhibitor.Profiles.Infrastructure
  |-- Exhibitor.Profiles.PublicApi             <- implements IProfileModuleApi
  +-- Exhibitor.Common.Application

Exhibitor.Profiles.Infrastructure
  |-- Exhibitor.Profiles.Domain
  +-- Exhibitor.Common.Cosmos
```

> **Rule:** The Integration project only interacts with modules through **PublicApi interfaces**. It never references Features or Infrastructure directly.

---

## Adding Cross-Module Communication Later (Phase 2)

When the Brands module is added, it will reference `Exhibitor.Profiles.PublicApi` for cross-module calls. `IProfileModuleApi` can be extended with additional methods:

```csharp
// Future additions to IProfileModuleApi
Task<bool> ExhibitorExistsAsync(string exhibitorId, CancellationToken ct = default);
Task<ExhibitorSummary?> GetExhibitorSummaryAsync(string exhibitorId, CancellationToken ct = default);
```

The Profiles module never references Brands directly. Cross-module dependencies are always through PublicApi interfaces.

---

## Related Documents

- [00-overview.md](00-overview.md) -- Architecture overview
- [02-module-mapping.md](02-module-mapping.md) -- Module structure & service layer
- [06-publish-workflow.md](06-publish-workflow.md) -- Publish workflow
