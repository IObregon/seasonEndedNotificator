using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Notifications;

public sealed class SendDigestCommand(AppDbContext context, IEmailSender emailSender)
{
    public async Task<SendDigestResult> ExecuteAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var deliveryDto = await context.DigestDeliveries
            .Where(d => d.Id == deliveryId)
            .Select(d => new { d.Id, d.UserId, d.Status, d.Channel, d.DigestDate, ItemCount = d.Items.Count, AttemptCount = d.Attempts.Count })
            .FirstOrDefaultAsync(cancellationToken);

        if (deliveryDto is null || deliveryDto.Status is not ("Pending" or "Failed"))
            return new SendDigestResult(Sent: false, Reason: deliveryDto is null ? "NotFound" : "NotPending");

        var user = await context.Users.FindAsync([deliveryDto.UserId], cancellationToken);
        if (user is null || user.Status != "Active")
            return new SendDigestResult(Sent: false, Reason: "UserInactive");

        var candidates = await context.DigestItems
            .Where(item => item.DigestDeliveryId == deliveryId)
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
            await UpdateDeliveryStatusAsync(context, deliveryId, "Skipped", null, cancellationToken);
            return new SendDigestResult(Sent: false, Reason: "NoItems");
        }

        var attemptNumber = deliveryDto.AttemptCount + 1;
        DeliveryOutcome outcome;
        string? sanitizedError = null;

        try
        {
            var message = DigestMessages.Create(user.PreferredLanguage, user.Email, candidates);
            await emailSender.SendAsync(message, cancellationToken);
            outcome = DeliveryOutcome.Succeeded;
        }
        catch (HttpRequestException ex)
        {
            outcome = DeliveryOutcome.TransientFailure;
            sanitizedError = Sanitize(ex.Message);
        }
        catch (TimeoutException ex)
        {
            outcome = DeliveryOutcome.TransientFailure;
            sanitizedError = Sanitize(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            outcome = DeliveryOutcome.PermanentFailure;
            sanitizedError = Sanitize(ex.Message);
        }

        context.DeliveryAttempts.Add(new DeliveryAttempt
        {
            DigestDeliveryId = deliveryId,
            AttemptNumber = attemptNumber,
            Outcome = outcome.ToString(),
            SanitizedError = sanitizedError
        });

        var newStatus = outcome switch
        {
            DeliveryOutcome.Succeeded => "Sent",
            DeliveryOutcome.PermanentFailure => "PermanentlyFailed",
            _ => "Failed"
        };
        var nextAttempt = RetryPolicy.NextAttemptAt(attemptNumber, outcome);

        await UpdateDeliveryStatusAsync(context, deliveryId, newStatus, nextAttempt, cancellationToken);

        return new SendDigestResult(
            Sent: outcome == DeliveryOutcome.Succeeded,
            Reason: outcome == DeliveryOutcome.Succeeded ? null : outcome.ToString());
    }

    private static string Sanitize(string message) =>
        message.Length > 200 ? message[..200] : message;

    private static async Task UpdateDeliveryStatusAsync(
        AppDbContext context,
        Guid deliveryId,
        string status,
        DateTimeOffset? nextAttemptAt,
        CancellationToken cancellationToken)
    {
        var tracked = context.DigestDeliveries.Local.FirstOrDefault(d => d.Id == deliveryId);
        if (tracked is not null)
        {
            context.Entry(tracked).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }

        var delivery = await context.DigestDeliveries.FindAsync([deliveryId], cancellationToken);
        if (delivery is not null)
        {
            delivery.Status = status;
            delivery.NextAttemptAt = nextAttemptAt;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed record SendDigestResult(bool Sent, string? Reason);
