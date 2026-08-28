using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Notifications;

public sealed class TelegramRecipientQuery(AppDbContext context)
{
    public async Task<List<TelegramRecipient>> GetAsync()
    {
        return await context.Users
            .Where(user => user.Status == "Active" && user.TelegramNotificationsEnabled == true)
            .Join(context.TelegramDestinations,
                user => user.Id,
                dest => dest.UserId,
                (user, dest) => new TelegramRecipient(user.Id, dest.ChatId, user.PreferredLanguage))
            .ToListAsync();
    }
}

public sealed record TelegramRecipient(Guid UserId, long ChatId, string? PreferredLanguage);
