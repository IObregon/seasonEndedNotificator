using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace SeasonEnded.Api.Identity;

public sealed class RequestMagicLinkCommand(AppDbContext context, IEmailSender emailSender)
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

        await emailSender.SendAsync(new EmailMessage(
            normalized,
            "Sign in to Season Ended",
            $"Click here to sign in: {rawToken}",
            $"<p>Click here to sign in: <code>{rawToken}</code></p>"),
            CancellationToken.None);

        return new MagicLinkRequestResult(RawToken: rawToken);
    }
}

public sealed record MagicLinkRequestResult(string? RawToken);
