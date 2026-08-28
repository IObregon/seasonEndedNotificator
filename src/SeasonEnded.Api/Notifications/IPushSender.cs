namespace SeasonEnded.Api.Notifications;

public interface IPushSender
{
    Task<PushSendResult> SendAsync(PushSubscription subscription, string payload, CancellationToken cancellationToken = default);
}

public sealed record PushSendResult(bool Succeeded, int? StatusCode = null);

public sealed class UnconfiguredPushSender : IPushSender
{
    public Task<PushSendResult> SendAsync(PushSubscription subscription, string payload, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PushSendResult(false, 503));
}
