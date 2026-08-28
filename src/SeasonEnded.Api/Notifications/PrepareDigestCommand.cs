using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Notifications;

public sealed class PrepareDigestCommand(AppDbContext context)
{
    public async Task<List<DigestDelivery>> ExecuteAsync(DateOnly digestDate, CancellationToken cancellationToken = default)
    {
        var results = new List<DigestDelivery>();

        results.AddRange(await PrepareChannelAsync(
            "Email", digestDate,
            (await new EmailRecipientQuery(context).GetAsync()).Select(u => u.Id).ToList(),
            cancellationToken));

        results.AddRange(await PrepareChannelAsync(
            "Telegram", digestDate,
            (await new TelegramRecipientQuery(context).GetAsync()).Select(r => r.UserId).ToList(),
            cancellationToken));

        return results;
    }

    private async Task<List<DigestDelivery>> PrepareChannelAsync(
        string channel,
        DateOnly digestDate,
        List<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var results = new List<DigestDelivery>();

        foreach (var userId in userIds)
        {
            var existing = await context.DigestDeliveries
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d =>
                    d.UserId == userId &&
                    d.Channel == channel &&
                    d.DigestDate == digestDate, cancellationToken);

            if (existing is not null)
            {
                results.Add(existing);
                continue;
            }

            var candidates = await new DigestEligibilityQuery(context)
                .ForUserAsync(userId, digestDate);

            if (candidates.Count == 0)
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

            context.DigestDeliveries.Add(delivery);
            await context.SaveChangesAsync(cancellationToken);

            results.Add(delivery);
        }

        return results;
    }
}
