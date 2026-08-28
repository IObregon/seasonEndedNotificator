using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Jobs;

public sealed class DailyMetadataRefreshJob(
    AppDbContext context,
    IFollowedShowRefresh refresh)
{
    public const string JobName = "daily-metadata-refresh";
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(30);

    public async Task<DailyJobResult> RunAsync(
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var lease = await context.JobLeases.FindAsync([JobName], cancellationToken);
        if (lease is not null && lease.ExpiresAt > now && lease.Owner != owner)
            return DailyJobResult.LeaseUnavailable;

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
            var result = await refresh.ExecuteAsync(cancellationToken);
            execution.Status = result.Failed == 0 ? "Completed" : "CompletedWithFailures";
            execution.Refreshed = result.Refreshed;
            execution.Failed = result.Failed;
            await FinishAsync(execution, lease, now, cancellationToken);
            return DailyJobResult.Completed;
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

public enum DailyJobResult
{
    Completed,
    LeaseUnavailable
}
