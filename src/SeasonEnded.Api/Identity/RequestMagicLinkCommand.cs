using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace SeasonEnded.Api.Identity;

public sealed class RequestMagicLinkCommand(AppDbContext context, IEmailSender emailSender, string baseUrl)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    public async Task<MagicLinkRequestResult> ExecuteAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == normalized && u.Status == "Active");

        if (user is null)
            return new MagicLinkRequestResult(RawToken: null);

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

        var message = AuthenticationMessages.MagicLink(user.PreferredLanguage, rawToken, baseUrl) with
        {
            To = normalized
        };
        await emailSender.SendAsync(message, CancellationToken.None);

        return new MagicLinkRequestResult(RawToken: rawToken);
    }
}

public sealed record MagicLinkRequestResult(string? RawToken);
