namespace SeasonEnded.Api.Notifications;

public interface ITelegramSender
{
    Task<int> SendAsync(long chatId, string text, CancellationToken cancellationToken = default);
}

public sealed class UnconfiguredTelegramSender : ITelegramSender
{
    public Task<int> SendAsync(long chatId, string text, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Telegram sender is not configured.");
}
