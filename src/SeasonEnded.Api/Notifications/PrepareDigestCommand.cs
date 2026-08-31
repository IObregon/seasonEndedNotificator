using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Notifications;

public sealed class PrepareDigestCommand(AppDbContext context)
{
    public async Task<List<DigestDelivery>> ExecuteAsync(DateOnly digestDate, CancellationToken cancellationToken = default)
    {
        var results = new List<DigestDelivery>();

        var emailUserIds = (await new EmailRecipientQuery(context).GetAsync())
            .Select(u => u.Id).ToList();
        results.AddRange(await PrepareChannelAsync("Email", digestDate, emailUserIds, cancellationToken));

        var telegramUserIds = (await new TelegramRecipientQuery(context).GetAsync())
            .Select(r => r.UserId).ToList();
        results.AddRange(await PrepareChannelAsync("Telegram", digestDate, telegramUserIds, cancellationToken));

        var pushUserIds = await context.PushSubscriptions
            .Where(s => s.Active)
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        results.AddRange(await PrepareChannelAsync("Push", digestDate, pushUserIds, cancellationToken));

        return results;
    }

    private async Task<List<DigestDelivery>> PrepareChannelAsync(
        string channel,
        DateOnly digestDate,
        List<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return [];

        var existing = await context.DigestDeliveries
            .Include(d => d.Items)
            .Where(d => d.Channel == channel && d.DigestDate == digestDate && userIds.Contains(d.UserId))
            .ToListAsync(cancellationToken);

        var existingByUserId = existing.ToDictionary(d => d.UserId);
        var usersNeedingCheck = userIds
            .Where(id => !existingByUserId.ContainsKey(id))
            .ToList();

        var results = new List<DigestDelivery>(existing);

        if (usersNeedingCheck.Count == 0)
            return results;

        var alreadyDeliveredEventIds = await context.DigestItems
            .Join(context.DigestDeliveries,
                item => item.DigestDeliveryId,
                delivery => delivery.Id,
                (item, delivery) => new { item, delivery })
            .Where(x => usersNeedingCheck.Contains(x.delivery.UserId) && x.delivery.DigestDate <= digestDate)
            .Select(x => new { x.delivery.UserId, x.item.SeasonCompletionEventId })
            .ToListAsync(cancellationToken);

        var deliveredByUser = alreadyDeliveredEventIds
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.SeasonCompletionEventId).ToHashSet());

        var allCandidates = await context.ShowFollows
            .Where(follow => usersNeedingCheck.Contains(follow.UserId))
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
            .Select(item => new
            {
                item.follow.UserId,
                Candidate = new DigestCandidate(
                    item.show.Id,
                    item.show.Title,
                    item.season.Number,
                    item.season.EndDate,
                    item.completion.Id,
                    item.completion.CompletedAt,
                    item.show.ProviderId)
            })
            .ToListAsync(cancellationToken);

        var candidatesByUser = allCandidates
            .Where(x => !deliveredByUser.TryGetValue(x.UserId, out var delivered) || !delivered.Contains(x.Candidate.SeasonCompletionEventId))
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Candidate).ToList());

        var newDeliveries = new List<DigestDelivery>();
        foreach (var userId in usersNeedingCheck)
        {
            if (!candidatesByUser.TryGetValue(userId, out var candidates) || candidates.Count == 0)
                continue;

            var delivery = new DigestDelivery
            {
                UserId = userId,
                Channel = channel,
                DigestDate = digestDate,
                Status = "Pending"
            };

            foreach (var candidate in candidates)
            {
                delivery.Items.Add(new DigestItem
                {
                    SeasonCompletionEventId = candidate.SeasonCompletionEventId
                });
            }

            newDeliveries.Add(delivery);
        }

        if (newDeliveries.Count > 0)
        {
            context.DigestDeliveries.AddRange(newDeliveries);
            await context.SaveChangesAsync(cancellationToken);
        }

        results.AddRange(newDeliveries);
        return results;
    }
}
