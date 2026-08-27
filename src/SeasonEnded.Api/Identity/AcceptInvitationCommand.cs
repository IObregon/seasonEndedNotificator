using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace SeasonEnded.Api.Identity;

public sealed class AcceptInvitationCommand(AppDbContext context)
{
    public async Task<AcceptInvitationResult> ExecuteAsync(string rawToken)
    {
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var invitation = await context.Invitations
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash);

        if (invitation is null || invitation.Status != "Pending" || invitation.IsExpired(DateTime.UtcNow))
            return new AcceptInvitationResult(Succeeded: false, UserId: null, Email: null);

        var user = new User
        {
            Email = invitation.Email,
            Role = invitation.Role,
            Status = "Active"
        };

        context.Users.Add(user);
        invitation.Status = "Accepted";
        await context.SaveChangesAsync();

        return new AcceptInvitationResult(Succeeded: true, UserId: user.Id, Email: user.Email);
    }
}

public sealed record AcceptInvitationResult(bool Succeeded, Guid? UserId, string? Email);
