using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace SeasonEnded.Api.Identity;

public sealed class InviteUserCommand(AppDbContext context, IEmailSender emailSender)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    public async Task<InvitationResult> ExecuteAsync(string invitedByUserId, string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        var normalized = email.Trim().ToLowerInvariant();

        var existing = await context.Invitations
            .FirstOrDefaultAsync(i => i.Email == normalized && i.Status == "Pending");

        if (existing is not null)
        {
            if (existing.IsExpired(DateTime.UtcNow))
                existing.Status = "Expired";
            else
                return new InvitationResult(Created: false, RawToken: null);
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var invitation = new Invitation
        {
            Email = normalized,
            TokenHash = tokenHash,
            Role = UserRole.User,
            Status = "Pending",
            ExpiresAt = DateTime.UtcNow.Add(TokenLifetime),
            InvitedByUserId = invitedByUserId
        };

        context.Invitations.Add(invitation);
        await context.SaveChangesAsync();

        await emailSender.SendAsync(new EmailMessage(
            normalized,
            "You're invited to Season Ended",
            $"Accept your invitation with token: {rawToken}",
            $"<p>Accept your invitation with token: <code>{rawToken}</code></p>"),
            CancellationToken.None);

        return new InvitationResult(Created: true, RawToken: rawToken);
    }
}

public sealed record InvitationResult(bool Created, string? RawToken);
