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

        if (token is null || token.IsExpired(now))
        {
            if (token is not null) token.Status = "Expired";
            await context.SaveChangesAsync();
            return new ConsumeTelegramTokenResult(Succeeded: false);
        }

        var existingDest = await context.TelegramDestinations
            .FirstOrDefaultAsync(d => d.UserId == token.UserId);

        if (existingDest is not null)
        {
            token.Status = "Consumed";
            await context.SaveChangesAsync();
            return new ConsumeTelegramTokenResult(Succeeded: true);
        }

        token.Status = "Consumed";
        context.TelegramDestinations.Add(new TelegramDestination
        {
            UserId = token.UserId,
            ChatId = chatId,
            ConnectedAt = now
        });

        await context.SaveChangesAsync();
        return new ConsumeTelegramTokenResult(Succeeded: true);
    }
}

public sealed record ConsumeTelegramTokenResult(bool Succeeded);
