using Microsoft.EntityFrameworkCore;

namespace SeasonEnded.Api.Identity;

public sealed class RequestAccountDeletionCommand(AppDbContext context)
{
    public async Task<RequestAccountDeletionResult> ExecuteAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null)
            return RequestAccountDeletionResult.NotFound;
        if (user.Status == "PendingDeletion")
            return RequestAccountDeletionResult.AlreadyPending;

        if (user.Role == UserRole.Admin)
        {
            var anotherAdminExists = await context.Users.AnyAsync(candidate =>
                candidate.Id != userId &&
                candidate.Role == UserRole.Admin &&
                candidate.Status == "Active");
            if (!anotherAdminExists)
                return RequestAccountDeletionResult.LastActiveAdmin;
        }

        user.Status = "PendingDeletion";
        context.MagicLinkTokens.RemoveRange(
            context.MagicLinkTokens.Where(token => token.UserId == userId));
        context.Invitations.RemoveRange(
            context.Invitations.Where(invitation => invitation.Email == user.Email));
        await context.SaveChangesAsync();
        return RequestAccountDeletionResult.Pending;
    }
}

public enum RequestAccountDeletionResult
{
    Pending,
    AlreadyPending,
    NotFound,
    LastActiveAdmin
}
