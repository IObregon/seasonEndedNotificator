using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace SeasonEnded.Api.Identity;

public sealed class ConsumeMagicLinkCommand(AppDbContext context)
{
    public async Task<MagicLinkConsumeResult> ExecuteAsync(string rawToken)
    {
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var token = await context.MagicLinkTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (token is null || token.Status != "Pending" || token.IsExpired(DateTime.UtcNow))
            return new MagicLinkConsumeResult(Succeeded: false, UserId: null);

        var user = await context.Users.FindAsync(token.UserId);
        if (user is null || user.Status != "Active")
            return new MagicLinkConsumeResult(Succeeded: false, UserId: null);

        token.Status = "Consumed";
        await context.SaveChangesAsync();

        return new MagicLinkConsumeResult(Succeeded: true, UserId: user.Id);
    }
}

public sealed record MagicLinkConsumeResult(bool Succeeded, Guid? UserId);
