using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Catalog;

public sealed class UnfollowShowCommand(AppDbContext context)
{
    public async Task<UnfollowShowResult> ExecuteAsync(Guid userId, Guid showId)
    {
        var follow = await context.ShowFollows
            .FirstOrDefaultAsync(item => item.UserId == userId && item.ShowId == showId);
        if (follow is null)
            return new UnfollowShowResult(Removed: false);

        context.ShowFollows.Remove(follow);
        await context.SaveChangesAsync();
        return new UnfollowShowResult(Removed: true);
    }
}

public sealed record UnfollowShowResult(bool Removed);
