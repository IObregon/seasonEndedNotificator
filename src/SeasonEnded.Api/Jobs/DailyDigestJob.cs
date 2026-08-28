using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Jobs;

public sealed class DailyDigestJob(
    AppDbContext context,
    PrepareDigestCommand prepareDigest,
    SendDigestCommand sendDigest)
{
    public const string JobName = "daily-digest";
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(30);

    public async Task<DailyDigestResult> RunAsync(
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var lease = await context.JobLeases.FindAsync([JobName], cancellationToken);
        if (lease is not null && lease.ExpiresAt > now && lease.Owner != owner)
            return DailyDigestResult.LeaseUnavailable;

        if (lease is null)
        {
            lease = new JobLease { Name = JobName };
            context.JobLeases.Add(lease);
        }
        lease.Owner = owner;
        lease.ExpiresAt = now.Add(LeaseDuration);

        var execution = new JobExecution { JobName = JobName, StartedAt = now };
        context.JobExecutions.Add(execution);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            var digestDate = DateOnly.FromDateTime(now.Date);
            var deliveries = await prepareDigest.ExecuteAsync(digestDate, cancellationToken);
            var sentCount = 0;
            var failedCount = 0;

            foreach (var delivery in deliveries)
            {
                var result = await sendDigest.ExecuteAsync(delivery.Id, cancellationToken);
                if (result.Sent) sentCount++;
                else failedCount++;
            }

            execution.Status = failedCount == 0 ? "Completed" : "CompletedWithFailures";
            execution.Refreshed = sentCount;
            execution.Failed = failedCount;
            await FinishAsync(execution, lease, now, cancellationToken);
            return DailyDigestResult.Completed;
        }
        catch
        {
            execution.Status = "Failed";
            await FinishAsync(execution, lease, now, CancellationToken.None);
            throw;
        }
    }

    private async Task FinishAsync(
        JobExecution execution,
        JobLease lease,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        execution.CompletedAt = completedAt;
        lease.ExpiresAt = completedAt;
        await context.SaveChangesAsync(cancellationToken);
    }
}

public enum DailyDigestResult
{
    Completed,
    LeaseUnavailable
}
