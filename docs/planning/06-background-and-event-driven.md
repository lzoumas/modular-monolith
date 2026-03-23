# Background & Event-Driven Workloads

How to handle Service Bus triggers, Timer triggers, and other non-HTTP workloads in an ASP.NET Core host that is primarily an API.

---

## Problem Statement

The current Azure Functions apps get Service Bus triggers and Timer triggers "for free" — the Functions runtime handles message dispatch, retries, and scheduling. An ASP.NET Core Web API host has **no built-in equivalent**. We need a strategy for running background and event-driven work alongside the HTTP API.

---

## Current Usage (Needs Investigation)

Before choosing an approach, audit both source repos for non-HTTP triggers:

| Trigger Type | Profile Service | Brands Service | Notes |
|---|---|---|---|
| **ServiceBusTrigger** | 🔲 Investigate | 🔲 Investigate | Search for `ServiceBusTrigger`, `Platform.Shared.ServiceBus` usage |
| **TimerTrigger** | 🔲 Investigate | 🔲 Investigate | Search for `TimerTrigger`, scheduled/cron tasks |
| **BlobTrigger** | 🔲 Investigate | 🔲 Investigate | Unlikely, but check for file-processing triggers |
| **QueueTrigger** | 🔲 Investigate | 🔲 Investigate | Storage Queue triggers |

> **Action item:** Grep both repos for `[Function(`, `ServiceBusTrigger`, `TimerTrigger`, `BlobTrigger`, `QueueTrigger` to build the definitive list.

---

## Approaches

### Option A: `IHostedService` / `BackgroundService` (Recommended for most cases)

ASP.NET Core has a built-in abstraction for long-running background work. Each background task is a class that extends `BackgroundService` and runs on a separate thread inside the same process.

**How it works:**

```csharp
// Runs inside the same ASP.NET Core host alongside the API
public class BrandRequestProcessor : BackgroundService
{
    private readonly ServiceBusProcessor _processor;

    public BrandRequestProcessor(ServiceBusClient client)
    {
        _processor = client.CreateProcessor("brand-requests", "monolith-sub");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;
        await _processor.StartProcessingAsync(stoppingToken);

        // Keep alive until shutdown
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        // Deserialize, dispatch to handler (Platform.Shared.Mediator command)
        await args.CompleteMessageAsync(args.Message);
    }
}
```

**Registration in `Program.cs`:**
```csharp
builder.Services.AddHostedService<BrandRequestProcessor>();
```

**Pros:**
- Built into ASP.NET Core — zero extra dependencies
- Runs in-process — can inject the same services, repos, DbContexts
- Simple to understand and debug
- Supports both message listeners and periodic timers

**Cons:**
- If the API host crashes, background work stops too (same process)
- No built-in retry/dead-letter policies — you manage them via the Azure SDK
- Scaling is coupled to the API host (can't scale consumers independently)

---

### Option B: Separate Worker Service Project

A standalone `dotnet worker` project (no HTTP) dedicated to background processing.

```
ExhibitorPlatform.Host/          # API only
ExhibitorPlatform.Worker/        # Background jobs only
```

**How it works:**
```csharp
// ExhibitorPlatform.Worker/Program.cs
var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices(services =>
{
    services.AddProfilesInfrastructure();
    services.AddBrandsInfrastructure();
    services.AddHostedService<BrandRequestProcessor>();
    services.AddHostedService<ProfileCleanupJob>();
});

var host = builder.Build();
await host.RunAsync();
```

**Pros:**
- Clean separation — API and workers scale independently
- Worker crashes don't affect API availability
- Can deploy workers to different infra (Container App Job, AKS pod, etc.)

**Cons:**
- Two deployables to manage instead of one (partially defeats the monolith simplification goal)
- Shared code (domain, infra) needs to be project-referenced by both Host and Worker
- DI registration is duplicated across both programs

---

### Option C: Keep Azure Functions for Triggers Only (Hybrid)

Keep a lightweight Azure Functions app *only* for triggers. It receives the event and immediately forwards work to the monolith API via HTTP or a shared service layer.

```
ExhibitorPlatform.Host/            # Full API + business logic
ExhibitorPlatform.Triggers/       # Thin Azure Functions app — just triggers
```

**The Function becomes a thin forwarder:**
```csharp
[Function("ProcessBrandRequest")]
public async Task Run(
    [ServiceBusTrigger("brand-requests", "exhibitor-sub")] ServiceBusReceivedMessage message)
{
    // Forward to the monolith API
    var command = message.Body.ToObjectFromJson<ProcessBrandRequestCommand>();
    await _httpClient.PostAsJsonAsync("/internal/brands/process-request", command);
}
```

**Pros:**
- Zero changes to existing trigger code (minimal migration effort)
- Azure Functions handles retries, dead-lettering, scaling automatically
- Clear boundary: Functions = event ingress, Host = business logic

**Cons:**
- Still have an Azure Functions app to deploy and manage
- Network hop between Functions and Host
- Auth between the two apps
- Feels like keeping the microservice split for these specific flows

---

### Option D: Azure Container Apps Jobs (for Timer-Based Work)

If Timer triggers are being used for scheduled cleanup, data sync, or similar batch jobs, **Azure Container App Jobs** are a modern alternative.

**Pros:**
- Scale to zero (no cost when not running)
- Cron-based scheduling built in
- Can share the same container image as the API

**Cons:**
- More complex infrastructure (Container Apps required)
- Overkill if there are only 1-2 timer jobs

---

## Recommendation Matrix

| Scenario | Recommended Approach |
|---|---|
| **1-3 Service Bus consumers** | **Option A** — `BackgroundService` in the Host |
| **1-2 Timer/cron jobs** | **Option A** — `BackgroundService` with `PeriodicTimer` |
| **Heavy message processing (high throughput)** | **Option B** — Separate Worker Service |
| **Need independent scaling for consumers** | **Option B** — Separate Worker Service |
| **Want minimal migration effort for triggers** | **Option C** — Hybrid with Azure Functions |
| **Scheduled batch jobs at scale** | **Option D** — Container App Jobs |

---

## Timer Trigger Replacement Pattern

Azure Functions `TimerTrigger` → `BackgroundService` with `PeriodicTimer`:

```csharp
public class ProfileCleanupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(24));

    public ProfileCleanupJob(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CleanupDeletedProfilesCommand, Result>>();
            await handler.HandleAsync(new CleanupDeletedProfilesCommand(), stoppingToken);
        }
    }
}
```

---

## Service Bus Listener Pattern

Azure Functions `ServiceBusTrigger` → `BackgroundService` with `ServiceBusProcessor`:

```csharp
public class BrandEventListener : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly IServiceScopeFactory _scopeFactory;

    public BrandEventListener(ServiceBusClient client, IServiceScopeFactory scopeFactory)
    {
        _client = client;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processor = _client.CreateProcessor("brand-events", "monolith-sub", new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 5,
            AutoCompleteMessages = false
        });

        processor.ProcessMessageAsync += async args =>
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<ICommandHandler<ProcessBrandEventCommand, Result>>();

            var command = args.Message.Body.ToObjectFromJson<ProcessBrandEventCommand>();
            var result = await mediator.HandleAsync(command, args.CancellationToken);

            if (result.IsSuccess)
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            else
                await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        };

        processor.ProcessErrorAsync += args =>
        {
            // Log error
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
```

---

## Module Registration Pattern

Background services register via the module's `DependencyInjection.cs`, keeping the Host's `Program.cs` clean:

```csharp
// Exhibitor.Brands.Features/DependencyInjection.cs
public static IServiceCollection AddBrandsModule(this IServiceCollection services)
{
    // Handlers, validators, etc.
    services.AddTransient<ICommandHandler<ProcessBrandEventCommand, Result>, ProcessBrandEventHandler>();

    // Background services
    services.AddHostedService<BrandEventListener>();

    return services;
}
```

The Host doesn't need to know about individual background services — each module registers its own.

---

## Folder Structure for Background Features

Background workers follow the same vertical-slice pattern, just without an endpoint:

```
Features/
  ProcessBrandEvent/
    ProcessBrandEvent.cs            # Command + Handler (no endpoint)
    ProcessBrandEvent.Validators.cs
    ProcessBrandEvent.Mapping.cs
  CleanupExpiredRequests/
    CleanupExpiredRequests.cs       # Command + Handler (timer-driven)
```

The `BrandEventListener` or `CleanupJob` BackgroundService lives in the Features project root or a `BackgroundServices/` folder:

```
Exhibitor.Brands.Features/
  BackgroundServices/
    BrandEventListener.cs
    ExpiredRequestCleanupJob.cs
  Features/
    ProcessBrandEvent/
      ...
    CleanupExpiredRequests/
      ...
```

---

## Next Steps

1. **Audit source repos** for all non-HTTP triggers (see table above)
2. **Decide on approach** based on findings — likely Option A for a small number of triggers
3. **Update [02-module-mapping](02-module-mapping.md)** to include background service mapping
4. **Update [07-open-questions](07-open-questions.md)** — resolve question #10

---

## Related Documents

- [00-overview.md](00-overview.md) — Architecture overview
- [02-module-mapping.md](02-module-mapping.md) — Module structure (add background services)
- [07-open-questions.md](07-open-questions.md) — Question #10 (Service Bus) is addressed here
