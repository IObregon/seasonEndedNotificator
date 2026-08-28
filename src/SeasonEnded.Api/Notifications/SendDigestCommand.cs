using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Notifications;

public sealed class SendDigestCommand(AppDbContext context, IEmailSender emailSender)
{
    public async Task<SendDigestResult> ExecuteAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var delivery = await context.DigestDeliveries
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == deliveryId, cancellationToken);

        if (delivery is null || delivery.Status != "Pending")
            return new SendDigestResult(Sent: false, Reason: delivery is null ? "NotFound" : "NotPending");

        var user = await context.Users.FindAsync([delivery.UserId], cancellationToken);
        if (user is null || user.Status != "Active")
            return new SendDigestResult(Sent: false, Reason: "UserInactive");

        var candidates = await context.DigestItems
            .Where(item => item.DigestDeliveryId == delivery.Id)
            .Join(context.SeasonCompletionEvents,
                item => item.SeasonCompletionEventId,
                completion => completion.Id,
                (item, completion) => new { item, completion })
            .Join(context.Seasons,
                x => x.completion.SeasonId,
                season => season.Id,
                (x, season) => new { x.item, x.completion, season })
            .Join(context.Shows,
                x => x.season.ShowId,
                show => show.Id,
                (x, show) => new DigestCandidate(
                    show.Id, show.Title, x.season.Number, x.season.EndDate,
                    x.completion.Id, x.completion.CompletedAt, show.ProviderId))
            .ToListAsync();

        if (candidates.Count == 0)
        {
            delivery.Status = "Skipped";
            await context.SaveChangesAsync(cancellationToken);
            return new SendDigestResult(Sent: false, Reason: "NoItems");
        }

        var message = DigestMessages.Create(user.PreferredLanguage, user.Email, candidates);
        await emailSender.SendAsync(message, cancellationToken);

        delivery.Status = "Sent";
        await context.SaveChangesAsync(cancellationToken);

        return new SendDigestResult(Sent: true, Reason: null);
    }
}

public sealed record SendDigestResult(bool Sent, string? Reason);
