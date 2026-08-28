using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Notifications;

public sealed class DisconnectTelegramCommand(AppDbContext context)
{
    public async Task<bool> ExecuteAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null) return false;

        var dest = await context.TelegramDestinations
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (dest is not null)
            context.TelegramDestinations.Remove(dest);

        user.TelegramNotificationsEnabled = false;
        await context.SaveChangesAsync();
        return true;
    }
}
