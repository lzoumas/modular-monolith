# Background & Event-Driven Workloads

How the publish workflow runs across two entry points: the **API Host** (HTTP) and **Azure Functions** (Service Bus).

---

## Architecture

Two deployable projects share the same module code. The API Host handles HTTP requests. Azure Functions handles Service Bus messages. Both resolve services from the same module DI registrations — no HTTP hop between them.

```
                 ??????????????????????????????
  HTTP ????????? ?   ExhibitorPlatform.Host   ????
                 ??????????????????????????????  ?
                                                 ?  project reference
                                                 ?
                                    ??????????????????????????
                                    ?   Shared Module Code   ?
                                    ?                        ?
                                    ?  Profiles.Features     ?
                                    ?  Profiles.Infra        ?
                                    ?  Common.*              ?
                                    ??????????????????????????
                                                 ?
                                                 ?  project reference
                 ??????????????????????????????  ?
  Service Bus ?? ? ExhibitorPlatform.Functions????
                 ??????????????????????????????
```

**Why Azure Functions instead of `BackgroundService`?**

The publish workflow sends data to an **external system**. If that system is down, Azure Functions automatically retries the message and dead-letters it after max attempts. With `BackgroundService` you'd build all of that yourself. Functions also scale independently from the API host.

---

## Publish Flow (Profiles)

```
User clicks "Publish"
        ?
        ?
????????????????????????
?  ExhibitorPlatform   ?
?       .Host          ?
?                      ?
?  POST /profiles/     ?
?  {exhibitorId}/      ?
?  {profileId}/publish ???????? Service Bus: "profile-publish" queue
?                      ?        Payload: { exhibitorId, profileId, publishedBy }
?  Returns: 202        ?
?  Accepted            ?
????????????????????????
                                        ?
                                        ?
                              ????????????????????????
                              ?  ExhibitorPlatform   ?
                              ?     .Functions       ?
                              ?                      ?
                              ?  ServiceBusTrigger   ?
                              ?  "profile-publish"   ?
                              ?                      ?
                              ?  1. Publish profile  ???? IProfileModuleApi.PublishAsync()
                              ?  2. Transform        ?
                              ?  3. Send to external ???? HTTP to external system
                              ????????????????????????
```

---

## The API Endpoint

The publish endpoint is thin — validate, drop a message on the queue, return `202 Accepted`. The actual publish logic runs asynchronously in the Function.

```csharp
// Exhibitor.Profiles.Features/Features/PublishProfile/PublishProfileEndpoint.cs

public sealed class PublishProfileEndpoint : EndpointWithoutRequest
{
    private readonly ServiceBusSender _sender;

    public PublishProfileEndpoint(ServiceBusSender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Post("/api/profiles/{exhibitorId}/{profileId}/publish");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var exhibitorId = Route<string>("exhibitorId")!;
        var profileId = Route<string>("profileId")!;
        var publishedBy = User.Identity?.Name ?? "system";

        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(new PublishProfileMessage
        {
            ExhibitorId = exhibitorId,
            ProfileId = profileId,
            PublishedBy = publishedBy
        }));

        await _sender.SendMessageAsync(message, ct);

        await SendAsync(null, 202, ct);
    }
}
```

---

## The Azure Function

The Function is the orchestrator. It calls `IProfileModuleApi` to publish the profile, transforms the result, and sends it to the external system.

```csharp
// ExhibitorPlatform.Functions/Functions/Profiles/PublishProfileFunction.cs

public class PublishProfileFunction
{
    private readonly IProfileModuleApi _profileApi;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PublishProfileFunction> _logger;

    public PublishProfileFunction(
        IProfileModuleApi profileApi,
        IHttpClientFactory httpClientFactory,
        ILogger<PublishProfileFunction> logger)
    {
        _profileApi = profileApi;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [Function("PublishProfile")]
    public async Task Run(
        [ServiceBusTrigger("profile-publish", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        FunctionContext context)
    {
        var payload = message.Body.ToObjectFromJson<PublishProfileMessage>();

        _logger.LogInformation("Publishing profile {ProfileId} for exhibitor {ExhibitorId}",
            payload.ProfileId, payload.ExhibitorId);

        // 1. Publish the profile (draft ? published) via PublicApi
        var publishResult = await _profileApi.PublishAsync(
            payload.ExhibitorId, payload.ProfileId, payload.PublishedBy);

        if (!publishResult.IsSuccess)
        {
            _logger.LogError("Failed to publish profile {ProfileId}: {Errors}",
                payload.ProfileId, string.Join(", ", publishResult.Errors));
            throw new InvalidOperationException($"Publish failed for profile {payload.ProfileId}");
            // Throwing ? message retried ? dead-lettered after max attempts
        }

        var published = publishResult.Value;

        // 2. Transform for external system
        var externalPayload = new ExternalProfilePayload
        {
            ExhibitorId = published.ExhibitorId,
            CompanyName = published.CompanyName,
            Contact = new ExternalContact
            {
                FirstName = published.Content.ShowroomContact.FirstName,
                LastName = published.Content.ShowroomContact.LastName,
                Email = published.Content.ShowroomContact.Email,
                Phone = published.Content.ShowroomContact.Phone
            },
            Website = published.Content.CompanyContact.Website,
            PublishedAt = published.PublishedOn
        };

        // 3. Send to external system
        var client = _httpClientFactory.CreateClient("ExternalCatalogApi");
        var response = await client.PostAsJsonAsync("/api/exhibitors/sync", externalPayload);
        response.EnsureSuccessStatusCode();
        // If this throws ? message retried automatically

        _logger.LogInformation("Profile {ProfileId} published and synced to external system",
            payload.ProfileId);
    }
}
```

---

## Message Contract

Lives in the PublicApi project so both the Host (sender) and Functions (receiver) can reference it.

```csharp
// Exhibitor.Profiles.PublicApi/Messages/PublishProfileMessage.cs
namespace Exhibitor.Profiles.PublicApi.Messages;

public record PublishProfileMessage
{
    public string ExhibitorId { get; init; } = string.Empty;
    public string ProfileId { get; init; } = string.Empty;
    public string PublishedBy { get; init; } = string.Empty;
}
```

---

## IProfileModuleApi

The Functions project accesses the Profiles module **only** through this interface. The implementation (`ProfileModuleApi`) delegates to `IProfileService` internally.

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

```csharp
// Exhibitor.Profiles.PublicApi/Contracts/PublishedProfile.cs
namespace Exhibitor.Profiles.PublicApi.Contracts;

public record PublishedProfile(
    string ProfileId,
    string ExhibitorId,
    string CompanyName,
    ProfileContentSnapshot Content,
    DateTime PublishedOn,
    string PublishedBy);

public record ProfileContentSnapshot(
    ShowroomContactInfo ShowroomContact,
    CompanyContactInfo CompanyContact,
    SocialMediaInfo SocialMedia,
    ShowroomPreferencesInfo ShowroomPreferences,
    int[] ChannelIds);

public record ShowroomContactInfo(string FirstName, string LastName, string Email, string Phone);
public record CompanyContactInfo(string PhoneNumber, string PrimaryEmail, string Website, string City, string State);
public record SocialMediaInfo(string? LinkedInUrl, string? InstagramUrl, string? FacebookUrl);
public record ShowroomPreferencesInfo(int[] ShowroomDetails, int[] HoursOfOperation);
```

---

## Functions `Program.cs`

Registers the same module DI as the Host. The module code doesn't know which host is calling it.

```csharp
// ExhibitorPlatform.Functions/Program.cs
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Shared Cosmos infrastructure
        services.AddCosmosDbClient(configuration);

        // Profiles module — same registration as Host
        services.AddProfilesModule();
        services.AddProfilesInfrastructure(configuration);

        // External system HTTP client
        services.AddHttpClient("ExternalCatalogApi", client =>
        {
            client.BaseAddress = new Uri(configuration["ExternalSystems:CatalogApiUrl"]!);
        });
    })
    .Build();

host.Run();
```

---

## Where Everything Lives

```
Profiles/
  Exhibitor.Profiles.PublicApi/
    IProfileModuleApi.cs                     # PublishAsync, GetPublishedAsync
    Contracts/
      PublishedProfile.cs                    # Published snapshot DTOs
    Messages/
      PublishProfileMessage.cs               # Service Bus message contract

  Exhibitor.Profiles.Features/
    Services/
      IProfileService.cs                     # Internal service interface (CRUD + publish)
      ProfileService.cs                      # Business logic implementation
    Features/
      PublishProfile/
        PublishProfileEndpoint.cs            # FastEndpoints — sends SB message, returns 202
      DiscardDraft/
        DiscardDraftEndpoint.cs              # FastEndpoints — synchronous, no SB needed
      CreateProfile/
        CreateProfileEndpoint.cs
        CreateProfileValidator.cs
        CreateProfileMapping.cs
      GetProfile/
        GetProfileEndpoint.cs
        GetProfileMapping.cs
      UpdateProfile/
        ...
      DeleteProfile/
        ...
      ListProfiles/
        ...

  Exhibitor.Profiles.Domain/
    Entities/
      Profile.cs                             # Inherits PublishableEntity<ProfileContent>
      ProfileContent.cs

  Exhibitor.Profiles.Infrastructure/
    Repositories/
      ProfileRepository.cs
    Interfaces/
      IProfileRepository.cs

ExhibitorPlatform.Functions/
  Functions/
    Profiles/
      PublishProfileFunction.cs              # Orchestrates: publish ? transform ? send

Common/
  Exhibitor.Common.Application/
    Models/
      PublishableEntity.cs                   # Abstract base class
    Interfaces/
      IPublishableService.cs                 # Shared publish/discard interface
  Exhibitor.Common.Cosmos/
    Documents/
      PublishableDocument.cs                 # Abstract base class
```

---

## Related Documents

- [00-overview.md](00-overview.md) — Architecture overview
- [02-module-mapping.md](02-module-mapping.md) — Full module structure
- [03-integration-points.md](03-integration-points.md) — PublicApi interface design
- [Draft/Publish Pattern](https://github.com/innovationsandmore/experience.exhibitor.profile.service/blob/feature/UXP-3481/docs/draft-publish-pattern.md) — Source domain pattern
