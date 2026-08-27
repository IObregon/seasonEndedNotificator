using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Catalog;

public sealed class FollowShowCommand(AppDbContext context)
{
    public async Task<FollowShowResult> ExecuteAsync(Guid userId, Guid showId)
    {
        var existing = await context.ShowFollows
            .FirstOrDefaultAsync(follow => follow.UserId == userId && follow.ShowId == showId);
        if (existing is not null)
            return new FollowShowResult(Created: false, existing);

        var follow = new ShowFollow { UserId = userId, ShowId = showId };
        context.ShowFollows.Add(follow);
        await context.SaveChangesAsync();
        return new FollowShowResult(Created: true, follow);
    }
}

public sealed record FollowShowResult(bool Created, ShowFollow Follow);
