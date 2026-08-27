using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.SeasonTracking;

public sealed class CompletionCandidateQuery(AppDbContext context)
{
    public async Task<List<SeasonCompletionEvent>> ForUserAsync(Guid userId)
    {
        return await context.ShowFollows
            .Where(follow => follow.UserId == userId)
            .Join(context.Seasons,
                follow => follow.ShowId,
                season => season.ShowId,
                (follow, season) => new { follow, season })
            .Join(context.SeasonCompletionEvents,
                item => item.season.Id,
                completion => completion.SeasonId,
                (item, completion) => new { item.follow, completion })
            .Where(item => item.completion.CompletedAt > item.follow.FollowedAt)
            .Select(item => item.completion)
            .ToListAsync();
    }
}
