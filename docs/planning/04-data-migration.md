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

## Strategy Options

### Option A: Single Shared Database, Module-Owned Containers ✅ Recommended

```
Database: ExhibitorPlatformDb
  ├── profiles            (owned by Profiles module)
  ├── brands              (owned by Brands module)
  ├── exhibitorBrands     (owned by Brands module)
  └── brandRequests       (owned by Brands module)
```

**Pros:**
- Single `CosmosClient` instance, single connection pool
- Simpler configuration (one connection string)
- Each module still owns its own containers — clean boundaries
- Matches the "shared database, separate tables" pattern from the reference monolith (which uses PostgreSQL with per-module DbContexts)

**Cons:**
- Modules share throughput at the database level (mitigated by per-container throughput provisioning)
- Harder to split back into microservices later (but that's the point of consolidating)

### Option B: Separate Databases per Module

```
Database: ExhibitorProfilesDb
  └── profiles

Database: ExhibitorBrandsDb
  ├── brands
  ├── exhibitorBrands
  └── brandRequests
```

**Pros:**
- Maximum isolation
- Easy to split back out

**Cons:**
- Multiple `CosmosClient` instances or complex routing
- More configuration, more connection overhead
- Over-engineering for a monolith

### Option C: Single Container with Discriminator

Not recommended — forces all entities into one container, complicates queries, loses partition key optimization.

---

## Recommendation: Option A

Use a single database (`ExhibitorPlatformDb`) with module-owned containers. This aligns with the modular monolith philosophy: shared infrastructure, isolated business logic.

---

## Cosmos Client Setup in Monolith

The shared `CosmosClient` is registered once in the Host via `Exhibitor.Common.Cosmos`:

```csharp
// Program.cs
builder.Services.AddCosmosDbClient(builder.Configuration, containers:
[
    // Profiles module
    new ContainerDefinition("profiles", "/exhibitorId"),
    // Brands module
    new ContainerDefinition("brands", "/externalBrandId"),
    new ContainerDefinition("exhibitorBrands", "/exhibitorId"),
    new ContainerDefinition("brandRequests", "/exhibitorId"),
]);
```

Each module's Infrastructure project resolves `CosmosClient` and accesses only its own containers.

---

## Migration Path

### For Local Development
No migration needed — the Cosmos DB emulator starts fresh. Container definitions in `Program.cs` handle creation.

### For Deployed Environments

| Step | Action |
|---|---|
| 1 | Create the new `ExhibitorPlatformDb` database in Azure Cosmos DB |
| 2 | Create containers with matching partition keys |
| 3 | Migrate data from old databases using Azure Data Factory or custom script |
| 4 | Update app settings to point to new database |
| 5 | Decommission old Function Apps and old databases |

### Data Compatibility

The document schema stays the same — `CosmosDbDocument` base class (Id, PartitionKey, audit fields) is unchanged. No document-level migration needed; it's just moving documents between databases/containers.

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
