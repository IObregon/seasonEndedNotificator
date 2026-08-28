using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Jobs;

public abstract class DailyJob(AppDbContext context)
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(30);

    protected abstract string LeaseKey { get; }

    protected async Task<JobExecution?> AcquireLeaseAndStartAsync(
        string owner, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var lease = await context.JobLeases.FindAsync([LeaseKey], cancellationToken);
        if (lease is not null && lease.ExpiresAt > now && lease.Owner != owner)
            return null!;

        if (lease is null)
        {
            lease = new JobLease { Name = LeaseKey };
            context.JobLeases.Add(lease);
        }
        lease.Owner = owner;
        lease.ExpiresAt = now.Add(LeaseDuration);

        var execution = new JobExecution { JobName = LeaseKey, StartedAt = now };
        context.JobExecutions.Add(execution);
        await context.SaveChangesAsync(cancellationToken);

        return execution;
    }

    protected async Task FinishAsync(
        Guid executionId, DateTimeOffset completedAt, string status,
        int refreshed, int failed, CancellationToken cancellationToken)
    {
        var lease = await context.JobLeases.FindAsync([LeaseKey], cancellationToken);
        if (lease is not null) lease.ExpiresAt = completedAt;

        var execution = await context.JobExecutions.FindAsync([executionId], cancellationToken);
        if (execution is not null)
        {
            execution.Status = status;
            execution.CompletedAt = completedAt;
            execution.Refreshed = refreshed;
            execution.Failed = failed;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    protected static bool IsLeaseUnavailable(JobExecution? execution) => execution is null;
}
