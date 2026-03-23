# Integration Points

How the Functions project and future modules interact with the Profiles module.

---

## PublicApi Interface

The `IProfileModuleApi` interface is the **only** way code outside the Profiles module accesses profile data. The Functions project uses it for the publish workflow. Future modules (e.g. Brands) will use it for cross-module queries.

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
  |-- Exhibitor.Profiles.Features
  |-- Exhibitor.Profiles.Infrastructure
  |-- Exhibitor.Common.*
  +-- Platform.Shared.* (external, if needed)

ExhibitorPlatform.Functions
  |-- Exhibitor.Profiles.PublicApi              <- uses IProfileModuleApi in Function classes
  |-- Exhibitor.Profiles.Features              <- for DI registration (AddProfilesModule)
  |-- Exhibitor.Profiles.Infrastructure        <- for DI registration (AddProfilesInfrastructure)
  +-- Exhibitor.Common.*

Exhibitor.Profiles.Features
  |-- Exhibitor.Profiles.Domain
  |-- Exhibitor.Profiles.Infrastructure
  |-- Exhibitor.Profiles.PublicApi             <- implements IProfileModuleApi
  +-- Exhibitor.Common.Application

Exhibitor.Profiles.Infrastructure
  |-- Exhibitor.Profiles.Domain
  +-- Exhibitor.Common.Cosmos
```

> **Rule:** Function classes only interact with modules through **PublicApi interfaces**. The Features/Infrastructure references are only for DI registration (`AddProfilesModule()`).

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
- [06-background-and-event-driven.md](06-background-and-event-driven.md) -- Publish workflow
