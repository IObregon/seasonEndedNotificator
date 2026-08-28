using Microsoft.EntityFrameworkCore;
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

        var candidates = new List<DigestCandidate>();
        foreach (var item in delivery.Items)
        {
            var completionEvent = await context.SeasonCompletionEvents
                .FirstOrDefaultAsync(e => e.Id == item.SeasonCompletionEventId, cancellationToken);
            if (completionEvent is null) continue;

            var season = await context.Seasons
                .FirstOrDefaultAsync(s => s.Id == completionEvent.SeasonId, cancellationToken);
            if (season is null) continue;

            var show = await context.Shows
                .FirstOrDefaultAsync(s => s.Id == season.ShowId, cancellationToken);
            if (show is null) continue;

            candidates.Add(new DigestCandidate(
                show.Id, show.Title, season.Number, season.EndDate,
                completionEvent.Id, completionEvent.CompletedAt, show.ProviderId));
        }

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
