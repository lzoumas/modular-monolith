# Open Questions

Decisions and unknowns to resolve before or during migration.

---

## Architecture Decisions

### 1. Endpoint Framework
**Status:** 🟡 Needs decision

| Option | Notes |
|---|---|
| **Minimal APIs** | Built-in, no extra dependency. Recommended — see [02-module-mapping](02-module-mapping.md). |
| **Carter** | Used in the reference monolith. Adds `ICarterModule` per feature for auto-discovery. |
| **FastEndpoints** | One class per endpoint, built-in validation/binding. More opinionated. |

**Recommendation:** Minimal APIs. The handler pattern from `Platform.Shared.Mediator` stays unchanged — only the thin HTTP "shell" changes. No need for an extra framework.

### 2. Result Pattern: `Ardalis.Result` vs `ErrorOr`
**Status:** 🟡 Needs decision

The current services use `Ardalis.Result`. The reference monolith uses `ErrorOr`.

| Option | Notes |
|---|---|
| **Keep Ardalis.Result** | Zero changes to existing handler logic. Already well-integrated. |
| **Switch to ErrorOr** | Matches reference monolith. Requires touching every handler. |

**Recommendation:** Keep `Ardalis.Result`. It works, the handlers already return it, and switching adds risk with no functional benefit.

### 3. `Platform.Shared.Mediator` vs MediatR
**Status:** 🟡 Needs decision

The reference monolith uses `MediatR`. The current services use the custom `Platform.Shared.Mediator`.

| Option | Notes |
|---|---|
| **Keep Platform.Shared.Mediator** | Zero changes to handlers. Direct handler injection (no dispatch). |
| **Switch to MediatR** | Pipeline behaviors, auto-registration, broader ecosystem. Requires rewriting all handlers. |

**Recommendation:** Keep `Platform.Shared.Mediator`. It's simpler (direct injection), already works, and avoids a rewrite. MediatR can be adopted later if pipeline behaviors become necessary.

---

## Data Questions

### 4. Cosmos DB: Shared vs Separate Databases
**Status:** ✅ Recommended Option A (shared database)

See [04-data-migration](04-data-migration.md).

### 5. Partition Key Strategy
**Status:** 🟢 Keep as-is

Current partition keys are well-chosen for the access patterns:
- `profiles` → `/exhibitorId`
- `brands` → `/externalBrandId`
- `exhibitorBrands` → `/exhibitorId`
- `brandRequests` → `/exhibitorId`

No changes needed.

### 6. Production Data Migration Timing
**Status:** 🔲 Needs planning

When do we cut over? Options:
- **Big bang:** Migrate data, switch DNS, decommission old services in one go
- **Parallel run:** Run both old and new simultaneously, sync data, then cut over
- **Phased:** Migrate Profiles first, then Brands

---

## Infrastructure Questions

### 7. Hosting: App Service vs Container App
**Status:** 🔲 Needs decision

| Option | Notes |
|---|---|
| **Azure App Service** | Simple, familiar, easy scaling |
| **Azure Container Apps** | Docker-based, better for complex deployments |
| **AKS** | Overkill for a single monolith |

### 8. Authentication / Authorization
**Status:** 🔲 Needs investigation

Current Function Apps likely use Function Keys or Azure AD. The Web API will need:
- Azure AD / Entra ID bearer tokens?
- API key middleware?
- Same auth as current Functions?

### 9. Swagger / OpenAPI Setup
**Status:** 🟢 Straightforward

Use `Swashbuckle.AspNetCore` (already in reference monolith). Confirm that Swagger groups/tags match the current API docs organization.

---

## Code Questions

### 10. What Service Bus functionality is needed?
**Status:** 🔲 Needs investigation

`Platform.Shared.ServiceBus` is available. Does either service publish or consume Service Bus messages? If so, that logic moves to the Host or to a shared background service.

### 11. What external HTTP calls exist?
**Status:** 🔲 Needs investigation

Search for `HttpClient` / `ManagedIdentityApiClient` usage in both services. These may need to remain as HTTP calls (they're to external services, not between exhibitor modules).

### 12. File Upload Endpoint Pattern
**Status:** 🔲 Needs investigation

The Brands service uses `Platform.Shared.Functions.Helpers.FileValidationHelpers` for multipart form parsing. In ASP.NET Core, this becomes `IFormFile` binding — need to adapt the upload endpoint accordingly.

### 13. Remove Reference Monolith Demo Code?
**Status:** 🟡 Needs decision

The current repo has demo modules (Shipments, Carriers, Stocks). Should we:
- **Remove them** and start clean with the exhibitor modules?
- **Keep them** as reference examples alongside the real modules?

**Recommendation:** Remove them. They've served their purpose as a pattern reference. The planning docs capture the patterns.

---

## Tracking

As questions are resolved, update the status:
- 🔲 Not started
- 🟡 Needs decision
- 🟢 Decided
- ✅ Implemented
