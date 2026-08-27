using Microsoft.EntityFrameworkCore;

namespace SeasonEnded.Api.Identity;

public sealed class ActiveUserPolicy(AppDbContext context)
{
    public Task<bool> CanUseSessionAsync(Guid userId) =>
        context.Users.AnyAsync(user => user.Id == userId && user.Status == "Active");

    public Task<List<Guid>> NotificationEligibleUserIdsAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.ToArray();
        return context.Users
            .Where(user => ids.Contains(user.Id) && user.Status == "Active")
            .Select(user => user.Id)
            .ToListAsync();
    }
}
