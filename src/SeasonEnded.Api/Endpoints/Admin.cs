using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Notifications;
using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api;

public static class AdminEndpoints
{
    public static WebApplication MapAdminEndpoints(this WebApplication app)
    {
        app.MapGet("/api/admin/metadata/issues", async (AppDbContext db) =>
        {
            var issues = await db.Seasons
                .Where(season => season.UncertaintyReason != null)
                .Join(db.Shows,
                    season => season.ShowId,
                    show => show.Id,
                    (season, show) => new MetadataIssueResponse(
                        show.ProviderId,
                        show.Title,
                        season.Number,
                        season.UncertaintyReason!.Value.ToString()))
                .OrderBy(issue => issue.Title)
                .ThenBy(issue => issue.SeasonNumber)
                .ToListAsync();

            return Results.Ok(issues);
        }).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        app.MapGet("/api/admin/delivery-failures", async (
            AppDbContext db,
            string? channel,
            string? status,
            DateOnly? fromDate,
            DateOnly? toDate,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default) =>
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = db.DigestDeliveries
                .Include(d => d.Attempts)
                .Where(d => d.Status == "Failed" || d.Status == "PermanentlyFailed");

            if (!string.IsNullOrEmpty(channel))
                query = query.Where(d => d.Channel == channel);
            if (!string.IsNullOrEmpty(status))
                query = query.Where(d => d.Status == status);
            if (fromDate.HasValue)
                query = query.Where(d => d.DigestDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(d => d.DigestDate <= toDate.Value);

            var total = await query.CountAsync(cancellationToken);
            var deliveries = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new
                {
                    d.Id,
                    d.UserId,
                    d.Channel,
                    d.DigestDate,
                    d.Status,
                    d.NextAttemptAt,
                    d.CreatedAt,
                    Attempts = d.Attempts.Select(a => new
                    {
                        a.AttemptNumber,
                        a.Outcome,
                        a.SanitizedError,
                        a.AttemptedAt
                    }).ToList()
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { total, page, pageSize, deliveries });
        }).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        app.MapGet("/api/admin/system-health", async (
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var lastRefresh = await db.JobExecutions
                .Where(e => e.JobName == "daily-metadata-refresh" && e.Status.StartsWith("Completed"))
                .OrderByDescending(e => e.CompletedAt)
                .Select(e => e.CompletedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var lastDigest = await db.JobExecutions
                .Where(e => e.JobName == "daily-digest" && e.Status.StartsWith("Completed"))
                .OrderByDescending(e => e.CompletedAt)
                .Select(e => e.CompletedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var pendingRetries = await db.DigestDeliveries
                .CountAsync(d => d.Status == "Failed" && d.NextAttemptAt != null, cancellationToken);

            var oldestRetry = await db.DigestDeliveries
                .Where(d => d.Status == "Failed" && d.NextAttemptAt != null)
                .OrderBy(d => d.NextAttemptAt)
                .Select(d => d.NextAttemptAt)
                .FirstOrDefaultAsync(cancellationToken);

            var failedDeliveries = await db.DigestDeliveries
                .CountAsync(d => d.Status == "PermanentlyFailed", cancellationToken);

            return Results.Ok(new
            {
                lastMetadataRefresh = lastRefresh,
                lastDigestRun = lastDigest,
                pendingRetries,
                oldestRetryNextAttempt = oldestRetry,
                permanentlyFailedDeliveries = failedDeliveries
            });
        }).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        app.MapPost("/api/admin/shows/{providerId:int}/refresh", async (
            int providerId,
            AppDbContext db,
            ITvShowDetails provider,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await new ImportShowDetailsCommand(db, provider)
                    .ExecuteAsync(providerId, cancellationToken);
                return Results.Ok(new { providerId, refreshed = true });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { providerId, refreshed = false, error = ex.Message.Length > 200 ? ex.Message[..200] : ex.Message });
            }
        }).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        app.MapPost("/api/admin/digests/send", async (
            AppDbContext db,
            IEmailSender emailSender,
            ITelegramSender telegramSender,
            IPushSender pushSender,
            CancellationToken cancellationToken) =>
        {
            var digestDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
            var prepared = await new PrepareDigestCommand(db).ExecuteAsync(digestDate, cancellationToken);
            var results = new List<object>();

            foreach (var delivery in prepared)
            {
                var result = await new SendDigestCommand(db, emailSender, telegramSender, pushSender)
                    .ExecuteAsync(delivery.Id, cancellationToken);
                results.Add(new { deliveryId = delivery.Id, sent = result.Sent, reason = result.Reason });
            }

            return Results.Ok(results);
        }).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        app.MapPost("/api/admin/digests/{deliveryId:guid}/retry", async (
            Guid deliveryId,
            AppDbContext db,
            IEmailSender emailSender,
            ITelegramSender telegramSender,
            IPushSender pushSender,
            CancellationToken cancellationToken) =>
        {
            var result = await new SendDigestCommand(db, emailSender, telegramSender, pushSender)
                .ExecuteAsync(deliveryId, cancellationToken);
            return Results.Ok(new { sent = result.Sent, reason = result.Reason });
        }).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        return app;
    }
}
