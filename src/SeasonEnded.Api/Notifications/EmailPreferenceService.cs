using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Notifications;

public sealed class EmailPreferenceService(AppDbContext context)
{
    public async Task<bool> IsEnabledAsync(Guid userId)
    {
        var preference = await context.Users
            .Where(user => user.Id == userId)
            .Select(user => user.EmailNotificationsEnabled)
            .SingleOrDefaultAsync();
        return preference ?? true;
    }

    public async Task<bool> SetAsync(Guid userId, bool enabled)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null || user.Status != "Active")
            return false;

        user.EmailNotificationsEnabled = enabled;
        await context.SaveChangesAsync();
        return true;
    }
}

public sealed class EmailRecipientQuery(AppDbContext context)
{
    public Task<List<User>> GetAsync() => context.Users
        .Where(user =>
            user.Status == "Active" &&
            user.EmailNotificationsEnabled != false)
        .ToListAsync();
}
