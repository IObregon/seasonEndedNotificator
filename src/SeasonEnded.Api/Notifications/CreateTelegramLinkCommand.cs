using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;
using System.Security.Cryptography;

namespace SeasonEnded.Api.Notifications;

public sealed class CreateTelegramLinkCommand(AppDbContext context)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    public async Task<TelegramLinkResult> ExecuteAsync(Guid userId, string botUsername)
    {
        var existing = await context.TelegramConnectionTokens
            .Where(t => t.UserId == userId && t.Status == "Pending")
            .ToListAsync();
        foreach (var token in existing)
            token.Status = "Revoked";

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = TelegramConnectionToken.HashToken(rawToken);

        context.TelegramConnectionTokens.Add(new TelegramConnectionToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime)
        });

        await context.SaveChangesAsync();

        return new TelegramLinkResult($"https://t.me/{botUsername}?start={rawToken}");
    }
}

public sealed record TelegramLinkResult(string DeepLink);
