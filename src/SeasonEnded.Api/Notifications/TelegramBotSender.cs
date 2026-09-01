using Microsoft.Extensions.Options;

namespace SeasonEnded.Api.Notifications;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";
    public string BotToken { get; init; } = "";
    public string BotUsername { get; init; } = "";
    public string WebhookSecret { get; init; } = "";
}

public sealed class TelegramBotSender(IHttpClientFactory httpClientFactory, IOptions<TelegramOptions> options) : ITelegramSender
{
    public async Task<int> SendAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        var http = httpClientFactory.CreateClient("TelegramBot");
        var payload = new
        {
            chat_id = chatId,
            text,
            parse_mode = "Markdown"
        };

        var response = await http.PostAsJsonAsync(
            $"/bot{options.Value.BotToken}/sendMessage", payload, cancellationToken);
        return response.IsSuccessStatusCode ? 1 : 0;
    }
}
