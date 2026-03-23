# Data Strategy

Cosmos DB setup for the modular monolith. This is greenfield -- there is no production data to migrate. The existing profile service has not gone past the dev environment.

---

## Database Layout

Single database (`ExhibitorPlatformDb`) with module-owned containers. Each module's Infrastructure project resolves `CosmosClient` and accesses only its own containers.

**Phase 1 (Profiles only):**
```
Database: ExhibitorPlatformDb
  +-- profiles            (owned by Profiles module, partition key: /exhibitorId)
```

**Phase 2 (adds Brands):**
```
Database: ExhibitorPlatformDb
  |-- profiles            (owned by Profiles module, partition key: /exhibitorId)
  |-- brands              (owned by Brands module, partition key: /externalBrandId)
  |-- exhibitorBrands     (owned by Brands module, partition key: /exhibitorId)
  +-- brandRequests       (owned by Brands module, partition key: /exhibitorId)
```

---

## Cosmos Client Setup

The shared `CosmosClient` is registered once in the WebApi via `Exhibitor.Common.Cosmos`:

```csharp
// Program.cs (Phase 1 -- Profiles only)
builder.Services.AddCosmosDbClient(builder.Configuration, containers:
[
    new ContainerDefinition("profiles", "/exhibitorId"),
]);
```

Phase 2 adds the Brands containers to this same registration.

---

## Local Development

Cosmos DB emulator via Docker (`docker-compose.yml`). Container definitions in `Program.cs` handle creation automatically -- no manual setup needed.

---

## Testing Strategy

Integration tests use `CosmosDbFixture` from `Exhibitor.Common.Cosmos.Testing` with Testcontainers. Each test run creates a fresh emulator with all required containers.

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
        ]
    })
    { }
}
