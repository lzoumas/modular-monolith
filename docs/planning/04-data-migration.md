# Data Migration

Cosmos DB strategy for the modular monolith.

---

## Current State

Each microservice has its own Cosmos DB database:

| Service | Database | Containers | Partition Key |
|---|---|---|---|
| Profile Service | `ExhibitorProfileDb` | `profiles` | `/exhibitorId` |
| Brands Service | `ExhibitorBrandsDb` | `brands`, `exhibitorBrands`, `brandRequests` | `/externalBrandId` |

Both use the Cosmos DB emulator locally and Azure Cosmos DB in deployed environments.

---

## Strategy

Single database (`ExhibitorPlatformDb`) with module-owned containers. This aligns with the modular monolith philosophy: shared infrastructure, isolated business logic. Each module's Infrastructure project resolves `CosmosClient` and accesses only its own containers.

**Phase 1 (Profiles only):**
```
Database: ExhibitorPlatformDb
  └── profiles            (owned by Profiles module, partition key: /exhibitorId)
```

**Phase 2 (adds Brands):**
```
Database: ExhibitorPlatformDb
  ├── profiles            (owned by Profiles module, partition key: /exhibitorId)
  ├── brands              (owned by Brands module, partition key: /externalBrandId)
  ├── exhibitorBrands     (owned by Brands module, partition key: /exhibitorId)
  └── brandRequests       (owned by Brands module, partition key: /exhibitorId)
```

---

## Cosmos Client Setup in Monolith

The shared `CosmosClient` is registered once in the Host via `Exhibitor.Common.Cosmos`:

```csharp
// Program.cs (Phase 1 -- Profiles only)
builder.Services.AddCosmosDbClient(builder.Configuration, containers:
[
    new ContainerDefinition("profiles", "/exhibitorId"),
]);
```

Phase 2 adds the Brands containers to this same registration.

---

## Migration Path

### For Local Development
No migration needed -- the Cosmos DB emulator starts fresh. Container definitions in `Program.cs` handle creation.

### For Deployed Environments

| Step | Action |
|---|---|
| 1 | Create the new `ExhibitorPlatformDb` database in Azure Cosmos DB |
| 2 | Create containers with matching partition keys |
| 3 | Migrate data from old databases using Azure Data Factory or custom script |
| 4 | Update app settings to point to new database |
| 5 | Decommission old Function Apps and old databases |

### Data Compatibility

The document schema stays the same -- `CosmosDbDocument` base class (Id, PartitionKey, audit fields) is unchanged. No document-level migration needed; it's just moving documents between databases/containers.

---

## Testing Strategy

Integration tests continue to use `CosmosDbFixture` from `Exhibitor.Common.Cosmos.Testing` with Testcontainers. Each test run creates a fresh emulator container with all required containers.

```csharp
public class ExhibitorPlatformCosmosDbFixture : CosmosDbFixture
{
    public ExhibitorPlatformCosmosDbFixture() : base(new CosmosDbFixtureOptions
    {
        DatabaseName = "ExhibitorPlatformTestDb",
        DockerContainerName = "exhibitor-platform-integration-cosmosdb",
        Containers =
        [
            new ContainerDefinition("profiles", "/exhibitorId"),
            new ContainerDefinition("brands", "/externalBrandId"),
            new ContainerDefinition("exhibitorBrands", "/exhibitorId"),
            new ContainerDefinition("brandRequests", "/exhibitorId"),
        ]
    })
    { }
}
```
