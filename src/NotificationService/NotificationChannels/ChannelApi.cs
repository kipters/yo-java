using Orleans.BroadcastChannel;
using Orleans.Runtime;

namespace NotificationService.NotificationChannels;

internal static class ChannelApi
{
    public static RouteGroupBuilder MapChannelApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/channels")
            .WithTags("channels");

        group
            .RequireAuthorization()
            .WithOpenApi();

        group
            .MapGet("/pubkey", () => new VapidPublicKeyResponse())
            .Produces<VapidPublicKeyResponse>()
            .WithName("Get Vapid Public Key")
            .WithDescription("Returns the public key used for VAPID authentication.");

        group
            .MapPut("/", (PushChannelRequest request) => Results.StatusCode(418))
            .Produces<PushRegistrationResponse>()
            .WithName("Register push channel")
            .WithDescription("Registers a push channel for the current user.");

        group
            .MapPost("{token}", (string token, PushChannelRequest request) => Results.StatusCode(418))
            .Produces<PushRegistrationResponse>()
            .WithName("Update push channel")
            .WithDescription("Updates a push channel for the current user.");

        group.MapDelete("{token}", (string token) => Results.StatusCode(418))
            .WithName("Delete push channel")
            .WithDescription("Deletes a push channel for the current user.");

        return group;
    }
}

public record VapidPublicKeyResponse();
public record PushChannelRequest(string Endpoint, string P256dh, string Auth);
public record PushRegistrationResponse(string Token);
[GenerateSerializer] public sealed record WebPushChannel(string Endpoint, string P256dh, string Auth);

public interface IPushGrain : IGrainWithStringKey
{
    Task<string> Subscribe(WebPushChannel notificationChannel);
    Task<string> UpdateSubscription(string registrationToken, WebPushChannel notificationChannel);
    Task Unsubscribe(string registrationToken);
    Task SendNotification(string notificationTitle, string notificationBody);
}

[GenerateSerializer]
public record NotificationModel(string Sender, string Target);

[ImplicitChannelSubscription]
internal sealed class WebPushGrain : Grain, IPushGrain, IOnBroadcastChannelSubscribed
{
    private readonly ILogger<WebPushGrain> _logger;
    private readonly IPersistentState<WebPushGrainState> _state;
    private readonly IVapidService _vapidService;

    public WebPushGrain(ILogger<WebPushGrain> logger, [PersistentState("web-push")] IPersistentState<WebPushGrainState> state, IVapidService vapidService)
    {
        _logger = logger;
        _state = state;
        _vapidService = vapidService;
    }

    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        streamSubscription.Attach<NotificationModel>(OnPublished, OnError);
        return Task.CompletedTask;
    }

    private async Task OnPublished(NotificationModel arg) => await SendNotification("Yo!", $"{arg.Sender} says Yo!");
    private Task OnError(Exception arg)
    {
        _logger.LogError(arg, "Error while receiving notification on BroadcastChannel");
        return Task.CompletedTask;
    }

    public async Task SendNotification(string notificationTitle, string notificationBody)
    {
        List<string> toRemove = new();

        foreach (var channel in _state.State.Channels)
        {
            var result = await _vapidService.SendMessage(channel.Value, notificationTitle, notificationBody);

            if (result == PushDeliveryResult.ChannelExpired)
            {
                toRemove.Add(channel.Key);
            }
        }

        if (toRemove.Count > 0)
        {
            foreach (var key in toRemove)
            {
                _state.State.Channels.Remove(key);
            }

            await _state.WriteStateAsync();
        }
    }

    public async Task<string> Subscribe(WebPushChannel notificationChannel)
    {
        var token = Guid.NewGuid().ToString();

        _state.State.Channels.Add(token, notificationChannel);
        await _state.WriteStateAsync();
        return token;
    }

    public async Task Unsubscribe(string registrationToken)
    {
        _state.State.Channels.Remove(registrationToken);
        await _state.WriteStateAsync();
    }

    public async Task<string> UpdateSubscription(string registrationToken, WebPushChannel notificationChannel)
    {
        if (!_state.State.Channels.ContainsKey(registrationToken))
        {
            return await Subscribe(notificationChannel);
        }

        _state.State.Channels[registrationToken] = notificationChannel;
        await _state.WriteStateAsync();

        return registrationToken;
    }
}

public interface IVapidService
{
    Task<PushDeliveryResult> SendMessage(WebPushChannel channe, string title, string body);
}

internal sealed class VapidService : IVapidService
{
    public Task<PushDeliveryResult> SendMessage(WebPushChannel channe, string title, string body)
    {
        return Task.FromResult(PushDeliveryResult.Sent);
    }
}

public enum PushDeliveryResult
{
    Sent, TemporaryFailure, InvalidRequest, ChannelExpired
}

internal sealed class WebPushGrainState
{
    public Dictionary<string, WebPushChannel> Channels { get; set; } = new();
}
