using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Notifications;

public sealed class RequestTelegramLoginCommand(
    AppDbContext context,
    ITelegramSender telegramSender,
    string baseUrl)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    public async Task<bool> ExecuteAsync(string email, long chatId)
    {
        var normalized = email.Trim().ToLowerInvariant();

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == normalized && u.Status == "Active");

        if (user is null)
            return false;

        var destination = await context.TelegramDestinations
            .FirstOrDefaultAsync(d => d.UserId == user.Id);

        if (destination is null)
        {
            context.TelegramDestinations.Add(new TelegramDestination
            {
                UserId = user.Id,
                ChatId = chatId,
                ConnectedAt = DateTimeOffset.UtcNow
            });
        }
        else if (destination.ChatId != chatId)
        {
            destination.ChatId = chatId;
        }

        await SendMagicLinkAsync(user, chatId);
        return true;
    }

    public async Task<bool> ExecuteAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == normalized && u.Status == "Active");

        if (user is null)
            return false;

        var destination = await context.TelegramDestinations
            .FirstOrDefaultAsync(d => d.UserId == user.Id);

        if (destination is null)
            return false;

        await SendMagicLinkAsync(user, destination.ChatId);
        return true;
    }

    private async Task SendMagicLinkAsync(User user, long chatId)
    {
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        context.MagicLinkTokens.Add(new MagicLinkToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            Status = "Pending",
            ExpiresAt = DateTime.UtcNow.Add(TokenLifetime)
        });
        await context.SaveChangesAsync();

        var loginUrl = $"{baseUrl.TrimEnd('/')}/?token={rawToken}";
        var isSpanish = user.PreferredLanguage == "es";

        var text = isSpanish
            ? $"🔑 *Inicia sesion en Season Ended*\n\nHaz clic en el siguiente enlace para iniciar sesion:\n{loginUrl}\n\nO usa este codigo: `{rawToken}`"
            : $"🔑 *Sign in to Season Ended*\n\nClick the following link to sign in:\n{loginUrl}\n\nOr use this code: `{rawToken}`";

        await telegramSender.SendAsync(chatId, text, CancellationToken.None);
    }
}
