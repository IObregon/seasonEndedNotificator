using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Notifications;

public sealed class ConsumeTelegramTokenCommand(AppDbContext context)
{
    public async Task<ConsumeTelegramTokenResult> ExecuteAsync(
        string rawToken, long chatId, DateTimeOffset now)
    {
        var tokenHash = TelegramConnectionToken.HashToken(rawToken);

        var token = await context.TelegramConnectionTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.Status == "Pending");

        if (token is null)
            return new ConsumeTelegramTokenResult(Succeeded: false);

        if (token.IsExpired(now))
        {
            token.Status = "Expired";
            await context.SaveChangesAsync();
            return new ConsumeTelegramTokenResult(Succeeded: false);
        }

        token.Status = "Consumed";

        var alreadyConnected = await context.TelegramDestinations
            .AnyAsync(d => d.UserId == token.UserId);

        if (!alreadyConnected)
        {
            context.TelegramDestinations.Add(new TelegramDestination
            {
                UserId = token.UserId,
                ChatId = chatId,
                ConnectedAt = now
            });
        }

        await context.SaveChangesAsync();
        return new ConsumeTelegramTokenResult(Succeeded: true);
    }
}

public sealed record ConsumeTelegramTokenResult(bool Succeeded);
