---
name: Technical Task
about: Set up local development environment
title: '[Tech] Local development environment setup'
labels: technical, project-setup
assignees: ''
---

## Summary

Set up Docker Compose for the Cosmos DB emulator, configure app settings for local development, and verify the WebApi project starts and connects to the local emulator.

## Background / Context

The existing demo used PostgreSQL via Docker. The exhibitor platform uses Cosmos DB. A Cosmos DB emulator (Linux) runs in Docker for local development. Container definitions in `Program.cs` handle automatic container creation on startup.

## Scope

### In Scope
- Docker Compose file with Cosmos DB emulator
- App settings for local development (Host)
- Cosmos DB health check verification
- README or docs update with local setup instructions

### Out of Scope
- Azure Service Bus emulator (use Azurite or connection to dev Service Bus)
- CI/CD pipeline setup
- Integration test fixtures (see profiles-module/07-integration-tests)

## Implementation Tasks

- [ ] Create/update `docker-compose.yml`:
  - Cosmos DB emulator (Linux image: `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest`)
  - Expose ports 8081 (HTTPS endpoint) and 10250--10255 (data ports)
  - Volume for persistent data (optional)
- [ ] Update `ExhibitorPlatform.WebApi/appsettings.Development.json`:
  - `CosmosDb:Endpoint` -> `https://localhost:8081`
  - `CosmosDb:Key` -> emulator default key
  - `CosmosDb:DatabaseName` -> `ExhibitorPlatformDb`
- [ ] Wire up `AddCosmosDbClient()` in WebApi `Program.cs` with initial container definition:
  - `profiles` container, partition key `/exhibitorId`
- [ ] Wire up Cosmos DB health check in WebApi `Program.cs`
- [ ] Add a `.env.example` or document required environment variables
- [ ] Update root `README.md` (or create one) with local setup steps:
  - `docker-compose up -d`
  - `dotnet run --project ExhibitorPlatform.WebApi`
  - How to access Scalar UI, health check endpoint

## Acceptance Criteria

- [ ] `docker-compose up -d` starts the Cosmos DB emulator
- [ ] WebApi project connects to the emulator
- [ ] `/health` endpoint returns healthy (Cosmos DB check passes)
- [ ] Developer can clone the repo, run docker-compose, and start the WebApi without manual Cosmos setup

## Files to Modify

| File | Change Type |
|------|-------------|
| `docker-compose.yml` | Add/Update |
| `ExhibitorPlatform.WebApi/appsettings.Development.json` | Update |
| `ExhibitorPlatform.WebApi/Program.cs` | Update |
| `README.md` | Add/Update |

## Risks / Considerations

- The Cosmos DB Linux emulator requires Docker with at least 3 GB RAM allocated. Document this.
- The emulator uses a self-signed SSL certificate. `CosmosClientOptions.HttpClientFactory` may need to be configured to bypass SSL validation in development.
- Service Bus is not covered here -- the publish workflow will need either Azurite or a dev Service Bus namespace.

## Verification Steps

1. Run `docker-compose up -d` -- emulator container starts
2. Run the WebApi -- connects to emulator
3. Hit `/health` -- returns healthy
4. Check Cosmos emulator data explorer at `https://localhost:8081/_explorer/index.html`
