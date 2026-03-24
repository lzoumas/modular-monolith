# Publish Workflow

How the publish workflow runs asynchronously: an API endpoint drops a Service Bus message, and a consumer processes it -- publishing profiles AND brands, transforming to a target schema, and sending to an external system.

---

## The Problem: Cross-Module Orchestration

The publish workflow is not a single-module concern. When an exhibitor publishes, the system needs to:

1. **Publish the profile** -- call `IProfileModuleApi.PublishAsync()` (draft -> published in Cosmos)
2. **Publish brands** -- call `IBrandModuleApi.PublishAsync()` (draft -> published in Cosmos)
3. **Transform** -- combine both results into the external system's target schema
4. **Send** -- HTTP POST to the external system

Steps 1--2 are module business logic (each module owns its own publish). Steps 3--4 are integration logic (combining cross-module data for an external consumer). This orchestration can't live inside either the Profiles or Brands module -- it spans both.

### Where previous options fall short

- **Option A (consumer inside a module)**: The orchestrator would need to call the OTHER module's PublicApi. The Profiles module would depend on the Brands PublicApi and vice versa. Neither module is the right owner.
- **Option B (separate Functions project)**: Solves the ownership problem (Functions project is the integration layer), but adds a second deployment, duplicate DI, and is no longer a true monolith.

### The answer: a dedicated Integration project

The orchestration is its own concern -- not owned by any module. A separate `Exhibitor.Integration` project sits between modules and external systems:

- References only module **PublicApi** interfaces (never internal module code)
- Owns the worker (`BackgroundService`), orchestrator, and external DTOs
- Runs in the WebApi process (single deployment, single DI container)

---

## Architecture

```
                  ExhibitorPlatform.WebApi (single host)
                  |
                  |  registers all modules + integration
                  v
+----------------------------------------------------------+
|                                                          |
|  Modules/Profiles/             Modules/Brands/           |
|  +--------------------+       +-------------------+      |
|  | IProfileModuleApi  |       | IBrandModuleApi   |      |
|  | - PublishAsync()   |       | - PublishAsync()  |      |
|  | - GetPublished()   |       | - GetPublished()  |      |
|  +--------------------+       +-------------------+      |
|          ^                            ^                  |
|          |                            |                  |
|          +------- both called by -----+                  |
|                       |                                  |
|  Integration/                                            |
|  +--------------------------------------------+         |
|  | ExhibitorPublishOrchestrator               |         |
|  |                                            |         |
|  | 1. IProfileModuleApi.PublishAsync()         |         |
|  | 2. IBrandModuleApi.PublishAsync()           |         |
|  | 3. Combine + transform to target schema    |         |
|  | 4. HTTP POST to external system            |         |
|  +--------------------------------------------+         |
|          ^                                               |
|          |                                               |
|  +--------------------------------------------+         |
|  | ExhibitorPublishWorker (BackgroundService)  |         |
|  | ServiceBusProcessor "exhibitor-publish"     |         |
|  +--------------------------------------------+         |
|                                                          |
+----------------------------------------------------------+
```

---

## Publish Flow

```
User clicks "Publish"
        |
        v
+---------------------------+
|  ExhibitorPlatform.WebApi |
|                           |
|  POST /api/exhibitors/    |
|  {exhibitorId}/publish    |----> Service Bus: "exhibitor-publish" queue
|                           |      Payload: { exhibitorId, publishedBy }
|  Returns: 202 Accepted    |
+---------------------------+
           |
           |  (same process)
           v
+----------------------------------------------+
|  ExhibitorPublishWorker (BackgroundService)  |
|  ServiceBusProcessor "exhibitor-publish"     |
|                                              |
|  ExhibitorPublishOrchestrator:               |
|    1. IProfileModuleApi.PublishAsync()        |<-- Profile module business logic
|    2. IBrandModuleApi.PublishAsync()          |<-- Brands module business logic
|    3. Combine + transform to target schema   |
|    4. HTTP POST to external system           |<-- IHttpClientFactory
+----------------------------------------------+
```

**Why Service Bus if it's the same process?**

The publish endpoint returns `202 Accepted` immediately. The actual work (publish to Cosmos, transform, POST to external) happens asynchronously. If the external system is slow or down, the user isn't waiting. Service Bus provides:

- **Durability** -- message survives process restarts
- **Retry** -- failed messages retried up to `MaxDeliveryCount`
- **Dead-letter** -- poison messages move to the dead-letter queue automatically
- **Decoupling** -- the endpoint doesn't need to know what happens after

These are queue-level features, not Azure Functions features. `ServiceBusProcessor` gets them all.

---

## The Publish Endpoint

Lives in the Integration project (not in a module) because it's an exhibitor-level action, not a profile-level or brand-level action.

```csharp
// Exhibitor.Integration/Features/PublishExhibitor/PublishExhibitorEndpoint.cs

public sealed class PublishExhibitorEndpoint : EndpointWithoutRequest
{
    private readonly ServiceBusSender _sender;

    public PublishExhibitorEndpoint(ServiceBusSender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Post("/api/exhibitors/{exhibitorId}/publish");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var exhibitorId = Route<string>("exhibitorId")!;
        var publishedBy = User.Identity?.Name ?? "system";

        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(
            new PublishExhibitorMessage
            {
                ExhibitorId = exhibitorId,
                PublishedBy = publishedBy
            }));

        await _sender.SendMessageAsync(message, ct);
        await SendAsync(null, 202, ct);
    }
}
```

---

## The Worker

A `BackgroundService` in the Integration project. Processes messages from the `exhibitor-publish` queue and delegates to the orchestrator.

```csharp
// Exhibitor.Integration/Workers/ExhibitorPublishWorker.cs

public sealed class ExhibitorPublishWorker : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExhibitorPublishWorker> _logger;

    public ExhibitorPublishWorker(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<ExhibitorPublishWorker> logger)
    {
        _processor = client.CreateProcessor("exhibitor-publish", new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 5,
            AutoCompleteMessages = false  // manual complete after success
        });
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _processor.ProcessMessageAsync += async args =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var orchestrator = scope.ServiceProvider
                .GetRequiredService<IExhibitorPublishOrchestrator>();

            var message = args.Message.Body
                .ToObjectFromJson<PublishExhibitorMessage>();

            await orchestrator.PublishAndSyncAsync(message, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        };

        _processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception,
                "Error processing message from {Queue}", args.EntityPath);
            return Task.CompletedTask;
        };

        await _processor.StartProcessingAsync(ct);
        await Task.Delay(Timeout.Infinite, ct);
        await _processor.StopProcessingAsync();
    }
}
```

---

## The Orchestrator

The core of the publish workflow. Calls both modules' PublicApis, combines the results, transforms to the target schema, and sends to the external system.

```csharp
// Exhibitor.Integration/Services/IExhibitorPublishOrchestrator.cs

public interface IExhibitorPublishOrchestrator
{
    Task PublishAndSyncAsync(PublishExhibitorMessage message, CancellationToken ct);
}

// Exhibitor.Integration/Services/ExhibitorPublishOrchestrator.cs

internal sealed class ExhibitorPublishOrchestrator(
    IProfileModuleApi profileApi,
    IBrandModuleApi brandApi,
    IHttpClientFactory httpClientFactory,
    ILogger<ExhibitorPublishOrchestrator> logger) : IExhibitorPublishOrchestrator
{
    public async Task PublishAndSyncAsync(PublishExhibitorMessage message, CancellationToken ct)
    {
        var exhibitorId = message.ExhibitorId;
        var publishedBy = message.PublishedBy;

        // 1. Publish profile (draft -> published)
        var profileResult = await profileApi.PublishAsync(exhibitorId, publishedBy, ct);

        if (!profileResult.IsSuccess)
            throw new InvalidOperationException(
                $"Profile publish failed for exhibitor {exhibitorId}: " +
                string.Join(", ", profileResult.Errors));

        logger.LogInformation("Profile published for exhibitor {ExhibitorId}", exhibitorId);

        // 2. Publish brands (draft -> published)
        var brandsResult = await brandApi.PublishAsync(exhibitorId, publishedBy, ct);

        if (!brandsResult.IsSuccess)
            throw new InvalidOperationException(
                $"Brands publish failed for exhibitor {exhibitorId}: " +
                string.Join(", ", brandsResult.Errors));

        logger.LogInformation("Brands published for exhibitor {ExhibitorId}", exhibitorId);

        // 3. Transform -- combine profile + brands into target schema
        var payload = MapToExternalPayload(profileResult.Value, brandsResult.Value);

        // 4. Send to external system
        var client = httpClientFactory.CreateClient("ExternalCatalogApi");
        var response = await client.PostAsJsonAsync("/api/exhibitors/sync", payload, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Exhibitor {ExhibitorId} synced to external catalog", exhibitorId);
    }

    private static ExternalExhibitorPayload MapToExternalPayload(
        PublishedProfile profile,
        PublishedBrands brands) => new()
    {
        ExhibitorId = profile.ExhibitorId,
        CompanyName = profile.CompanyName,
        Contact = new ExternalContact
        {
            FirstName = profile.Content.ShowroomContact.FirstName,
            LastName = profile.Content.ShowroomContact.LastName,
            Email = profile.Content.ShowroomContact.Email,
            Phone = profile.Content.ShowroomContact.Phone
        },
        Website = profile.Content.CompanyContact.Website,
        Brands = brands.Items.Select(b => new ExternalBrand
        {
            BrandId = b.BrandId,
            Name = b.Name,
            // ... brand mapping
        }).ToList(),
        PublishedAt = profile.PublishedOn
    };
}
```

---

## Message Contract

The publish message is exhibitor-level (not profile-level or brand-level). It lives in the Integration project since it's not owned by any single module.

```csharp
// Exhibitor.Integration/Messages/PublishExhibitorMessage.cs

public record PublishExhibitorMessage
{
    public string ExhibitorId { get; init; } = string.Empty;
    public string PublishedBy { get; init; } = string.Empty;
}
```

---

## Where Logic Lives

| Concern | Where | Why |
|---|---|---|
| Publish profile (draft -> published) | `ProfileService` in Profiles module | Module business rule |
| Publish brands (draft -> published) | `BrandService` in Brands module | Module business rule |
| Cross-module orchestration | `ExhibitorPublishOrchestrator` in Integration | Spans both modules -- neither module owns it |
| Transform to target schema | `ExhibitorPublishOrchestrator` in Integration | External system concern |
| Send to external system | `ExhibitorPublishOrchestrator` via `IHttpClientFactory` | HTTP config in `Program.cs` |
| Retry / dead-letter | Service Bus queue settings | Infrastructure -- `MaxDeliveryCount` |
| Message processing | `ExhibitorPublishWorker` in Integration | BackgroundService in WebApi process |

**Key principle:** Modules own their own publish business logic. The Integration project orchestrates across modules and handles external system communication. Modules have no idea an external system exists.

---

## Discard Draft

Discard is the inverse of publish -- it reverts draft content to the last published state. Unlike publish, discard is:

- **Synchronous** -- no Service Bus, no external system call
- **Single-module** -- you discard a profile draft or a brand draft, not both at once

So discard endpoints live inside their respective modules:

```
src/Modules/Profiles/Exhibitor.Profiles.Features/Features/DiscardDraft/DiscardDraftEndpoint.cs
src/Modules/Brands/Exhibitor.Brands.Features/Features/DiscardDraft/DiscardDraftEndpoint.cs
```

Each endpoint calls its own module's `IProfileService.DiscardDraftAsync()` or `IBrandService.DiscardDraftAsync()` directly.

---

## Where Everything Lives

```
src/Integration/
  Exhibitor.Integration/
    Exhibitor.Integration.csproj               # Refs: Profiles.PublicApi, Brands.PublicApi,
                                               #       FastEndpoints, Azure.Messaging.ServiceBus
    Features/
      PublishExhibitor/
        PublishExhibitorEndpoint.cs             # POST /api/exhibitors/{id}/publish -> SB message
    Workers/
      ExhibitorPublishWorker.cs                # BackgroundService -- processes SB messages
    Services/
      IExhibitorPublishOrchestrator.cs
      ExhibitorPublishOrchestrator.cs          # Calls both module PublicApis, transforms, sends
    Messages/
      PublishExhibitorMessage.cs               # Service Bus message payload
    Models/
      ExternalExhibitorPayload.cs              # Target schema for external system
    DependencyInjection.cs                     # AddIntegrationWorkflows()

src/Modules/Profiles/
  Exhibitor.Profiles.PublicApi/
    IProfileModuleApi.cs                       # PublishAsync(), GetPublishedAsync()
    Contracts/
      PublishedProfile.cs

  Exhibitor.Profiles.Features/
    Services/
      IProfileService.cs                       # CRUD + PublishAsync + DiscardDraftAsync
      ProfileService.cs
      ProfileModuleApi.cs                      # Implements IProfileModuleApi
    Features/
      CreateProfile/  GetProfile/  UpdateProfile/  DeleteProfile/  ListProfiles/
      DiscardDraft/
        DiscardDraftEndpoint.cs                # Synchronous -- module-internal

src/Modules/Brands/                            # Phase 2
  Exhibitor.Brands.PublicApi/
    IBrandModuleApi.cs                         # PublishAsync(), GetPublishedAsync()
    Contracts/
      PublishedBrands.cs

  Exhibitor.Brands.Features/
    Services/
      IBrandService.cs
      BrandService.cs
      BrandModuleApi.cs
    Features/
      DiscardDraft/
        DiscardDraftEndpoint.cs                # Synchronous -- module-internal

src/ExhibitorPlatform.WebApi/
  Program.cs                                   # Single host
```

---

## Project References

```
Exhibitor.Integration
  +-- Exhibitor.Profiles.PublicApi             # IProfileModuleApi
  +-- Exhibitor.Brands.PublicApi               # IBrandModuleApi
  +-- FastEndpoints                            # for PublishExhibitorEndpoint
  +-- Azure.Messaging.ServiceBus               # for worker + endpoint

ExhibitorPlatform.WebApi
  +-- Exhibitor.Integration                    # for DI registration + endpoint discovery
  +-- Exhibitor.Profiles.Features              # for AddProfilesModule()
  +-- Exhibitor.Profiles.Infrastructure        # for AddProfilesInfrastructure()
  +-- Exhibitor.Brands.Features                # for AddBrandsModule() (Phase 2)
  +-- Exhibitor.Brands.Infrastructure          # for AddBrandsInfrastructure() (Phase 2)
  +-- Exhibitor.Common.*
```

The Integration project references ONLY PublicApi interfaces -- never internal module code. It communicates with modules the same way any external consumer would.

---

## DI Registration

```csharp
// Exhibitor.Integration/DependencyInjection.cs

public static IServiceCollection AddIntegrationWorkflows(this IServiceCollection services)
{
    services.AddScoped<IExhibitorPublishOrchestrator, ExhibitorPublishOrchestrator>();
    services.AddHostedService<ExhibitorPublishWorker>();

    return services;
}
```

```csharp
// ExhibitorPlatform.WebApi/Program.cs

// Modules
builder.Services.AddProfilesModule();
builder.Services.AddProfilesInfrastructure(builder.Configuration);
// builder.Services.AddBrandsModule();            // Phase 2
// builder.Services.AddBrandsInfrastructure();     // Phase 2

// Integration workflows
builder.Services.AddIntegrationWorkflows();

// Service Bus
builder.Services.AddSingleton(
    new ServiceBusClient(builder.Configuration["ServiceBus:ConnectionString"]));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<ServiceBusClient>().CreateSender("exhibitor-publish"));

// External system HTTP client
builder.Services.AddHttpClient("ExternalCatalogApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalSystems:CatalogApiUrl"]!);
});
```

---

## Phase 1 vs Phase 2

In Phase 1 (Profiles only), the orchestrator calls only `IProfileModuleApi`. The `IBrandModuleApi` call is added in Phase 2 when the Brands module exists.

```csharp
// Phase 1 -- orchestrator calls profile only
var profileResult = await profileApi.PublishAsync(exhibitorId, publishedBy, ct);
// IBrandModuleApi not yet available -- skip or inject as optional

// Phase 2 -- add brands call
var brandsResult = await brandApi.PublishAsync(exhibitorId, publishedBy, ct);
// Combine both results in the transform step
```

The Integration project can handle this with constructor injection -- if `IBrandModuleApi` isn't registered yet (Phase 1), inject it as `null` or use a no-op implementation.

---

## Related Documents

- [00-overview.md](00-overview.md) -- Architecture overview
- [02-module-mapping.md](02-module-mapping.md) -- Full module structure
- [03-integration-points.md](03-integration-points.md) -- PublicApi interface design
