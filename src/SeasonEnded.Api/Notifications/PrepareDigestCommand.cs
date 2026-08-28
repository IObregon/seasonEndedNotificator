using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Notifications;

public sealed class PrepareDigestCommand(AppDbContext context)
{
    public async Task<List<DigestDelivery>> ExecuteAsync(DateOnly digestDate, CancellationToken cancellationToken = default)
    {
        var recipients = await new EmailRecipientQuery(context).GetAsync();
        var results = new List<DigestDelivery>();

        foreach (var user in recipients)
        {
            var existing = await context.DigestDeliveries
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d =>
                    d.UserId == user.Id &&
                    d.Channel == "Email" &&
                    d.DigestDate == digestDate, cancellationToken);

            if (existing is not null)
            {
                results.Add(existing);
                continue;
            }

            var candidates = await new DigestEligibilityQuery(context)
                .ForUserAsync(user.Id, digestDate);

            if (candidates.Count == 0)
                continue;

            var delivery = new DigestDelivery
            {
                UserId = user.Id,
                Channel = "Email",
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
