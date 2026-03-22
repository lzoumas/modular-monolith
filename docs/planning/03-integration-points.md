# Integration Points

Cross-module communication and PublicApi interface design.

---

## Current State: HTTP Calls Between Services

Today the Profile and Brands services are separate Azure Function apps. If they communicate at all, it's via HTTP calls using `Platform.Shared.ManagedIdentityApiClient`. This adds latency, requires auth configuration, and introduces network failure modes.

## Target State: In-Process via PublicApi Interfaces

In the modular monolith, modules communicate through **PublicApi interfaces** — simple C# interfaces resolved via DI. No network hop, no serialization, no auth overhead.

---

## Identified Integration Points

### Brands → Profiles

| Scenario | Why | PublicApi Method |
|---|---|---|
| Validate exhibitor exists when creating a brand | Brand needs to confirm the exhibitor has a profile | `IProfileModuleApi.ExhibitorExistsAsync(string exhibitorId)` |
| Get exhibitor summary for brand display | Brand listing may need exhibitor company name | `IProfileModuleApi.GetExhibitorSummaryAsync(string exhibitorId)` |

### Profiles → Brands

| Scenario | Why | PublicApi Method |
|---|---|---|
| Get brand count for exhibitor | Profile dashboard may show brand statistics | `IBrandModuleApi.GetBrandCountForExhibitorAsync(string exhibitorId)` |
| Delete exhibitor cascades | Deleting a profile may need to soft-delete associated brands | `IBrandModuleApi.SoftDeleteBrandsForExhibitorAsync(string exhibitorId)` |

> **Note:** These are hypothetical — the actual integration points need to be confirmed by reviewing the current service-to-service HTTP calls in the source code.

---

## PublicApi Interface Design

### `IProfileModuleApi`

```csharp
// Exhibitor.Profiles.PublicApi/IProfileModuleApi.cs
namespace Exhibitor.Profiles.PublicApi;

public interface IProfileModuleApi
{
    Task<bool> ExhibitorExistsAsync(string exhibitorId, CancellationToken cancellationToken = default);
    Task<ExhibitorSummary?> GetExhibitorSummaryAsync(string exhibitorId, CancellationToken cancellationToken = default);
}
```

```csharp
// Exhibitor.Profiles.PublicApi/Contracts/ExhibitorSummary.cs
namespace Exhibitor.Profiles.PublicApi.Contracts;

public record ExhibitorSummary(string ExhibitorId, string CompanyName, string Channel);
```

### `IBrandModuleApi`

```csharp
// Exhibitor.Brands.PublicApi/IBrandModuleApi.cs
namespace Exhibitor.Brands.PublicApi;

public interface IBrandModuleApi
{
    Task<int> GetBrandCountForExhibitorAsync(string exhibitorId, CancellationToken cancellationToken = default);
    Task SoftDeleteBrandsForExhibitorAsync(string exhibitorId, CancellationToken cancellationToken = default);
}
```

---

## Dependency Rules

```
ExhibitorPlatform.Host
  ├── references Exhibitor.Profiles.Features
  ├── references Exhibitor.Profiles.Infrastructure
  ├── references Exhibitor.Brands.Features
  ├── references Exhibitor.Brands.Infrastructure
  ├── references Exhibitor.Common.*
  └── references Platform.Shared.* (external)

Exhibitor.Profiles.Features
  ├── references Exhibitor.Profiles.Domain
  ├── references Exhibitor.Profiles.Infrastructure (for repos)
  ├── references Exhibitor.Brands.PublicApi        ← cross-module (PublicApi only!)
  └── references Exhibitor.Common.Application

Exhibitor.Brands.Features
  ├── references Exhibitor.Brands.Domain
  ├── references Exhibitor.Brands.Infrastructure (for repos)
  ├── references Exhibitor.Profiles.PublicApi      ← cross-module (PublicApi only!)
  └── references Exhibitor.Common.Application

Exhibitor.Profiles.Infrastructure
  ├── references Exhibitor.Profiles.Domain
  └── references Exhibitor.Common.Cosmos

Exhibitor.Brands.Infrastructure
  ├── references Exhibitor.Brands.Domain
  └── references Exhibitor.Common.Cosmos
```

### Rules
1. **Features** projects may reference their own Domain + Infrastructure, plus other modules' **PublicApi** only.
2. **Domain** projects have zero framework dependencies.
3. **Infrastructure** projects reference Domain + Common.Cosmos.
4. **PublicApi** projects have zero dependencies (just contracts + interface).
5. **No module ever references another module's Domain, Features, or Infrastructure.**

---

## Platform.Shared Integration

| Library | How It's Used |
|---|---|
| `Platform.Shared.Mediator` | Handlers implement `ICommandHandler<T, TResult>` / `IQueryHandler<T, TResult>`. Registered via `AddCommandHandler<>()` / `AddQueryHandler<>()` extensions. This stays exactly as-is. |
| `Platform.Shared.FileStorage` | Used by Brands module for media uploads. Referenced from `Exhibitor.Brands.Infrastructure` or `Exhibitor.Brands.Features`. |
| `Platform.Shared.ServiceBus` | If async events are needed between modules (e.g., "profile deleted" event). TBD. |
| `Platform.Shared.Functions` | **Not used** — replaced by ASP.NET Core middleware. The helpers/attributes are Azure Functions-specific. |
| `Platform.Shared.Extensions` | General utilities — referenced from Common or Features as needed. |
