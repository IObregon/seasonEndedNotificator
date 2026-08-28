using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Notifications;

public sealed class DigestEligibilityQuery(AppDbContext context)
{
    public async Task<List<DigestCandidate>> ForUserAsync(Guid userId, DateOnly digestDate)
    {
        var alreadyDeliveredEventIds = await context.DigestItems
            .Join(context.DigestDeliveries,
                item => item.DigestDeliveryId,
                delivery => delivery.Id,
                (item, delivery) => new { item, delivery })
            .Where(x => x.delivery.UserId == userId && x.delivery.DigestDate <= digestDate)
            .Select(x => x.item.SeasonCompletionEventId)
            .ToListAsync();

        return await context.ShowFollows
            .Where(follow => follow.UserId == userId)
            .Join(context.Seasons,
                follow => follow.ShowId,
                season => season.ShowId,
                (follow, season) => new { follow, season })
            .Join(context.Shows,
                item => item.season.ShowId,
                show => show.Id,
                (item, show) => new { item.follow, item.season, show })
            .Join(context.SeasonCompletionEvents,
                item => item.season.Id,
                completion => completion.SeasonId,
                (item, completion) => new { item.follow, item.season, item.show, completion })
            .Where(item => item.completion.CompletedAt > item.follow.FollowedAt)
            .Where(item => !alreadyDeliveredEventIds.Contains(item.completion.Id))
            .Select(item => new DigestCandidate(
                item.show.Id,
                item.show.Title,
                item.season.Number,
                item.season.EndDate,
                item.completion.Id,
                item.completion.CompletedAt,
                item.show.ProviderId))
            .ToListAsync();
    }
}

public sealed record DigestCandidate(
    Guid ShowId,
    string ShowTitle,
    int SeasonNumber,
    DateOnly? EndDate,
    Guid SeasonCompletionEventId,
    DateTimeOffset CompletedAt,
    int ShowProviderId);
