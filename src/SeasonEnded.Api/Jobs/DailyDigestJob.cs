using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Jobs;

public sealed class DailyDigestJob(IServiceScopeFactory scopeFactory)
{
    public const string JobName = "daily-digest";
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(30);

    public async Task<DailyDigestResult> RunAsync(
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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

        var executionId = execution.Id;

        int sentCount, failedCount;
        try
        {
            var digestDate = DateOnly.FromDateTime(now.Date);
            var prepare = scope.ServiceProvider.GetRequiredService<PrepareDigestCommand>();
            var deliveries = await prepare.ExecuteAsync(digestDate, cancellationToken);
            sentCount = 0;
            failedCount = 0;

            foreach (var delivery in deliveries)
            {
                var send = scope.ServiceProvider.GetRequiredService<SendDigestCommand>();
                var result = await send.ExecuteAsync(delivery.Id, cancellationToken);
                if (result.Sent) sentCount++;
                else failedCount++;
            }
        }
        catch
        {
            await RecordFinishAsync(scopeFactory, executionId, now, "Failed", 0, 0);
            throw;
        }

        await RecordFinishAsync(scopeFactory, executionId, now,
            failedCount == 0 ? "Completed" : "CompletedWithFailures",
            sentCount, failedCount);

        return DailyDigestResult.Completed;
    }

    private static async Task RecordFinishAsync(
        IServiceScopeFactory scopeFactory,
        Guid executionId,
        DateTimeOffset completedAt,
        string status,
        int refreshed,
        int failed)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var lease = await context.JobLeases.FindAsync([JobName]);
        if (lease is not null) lease.ExpiresAt = completedAt;

        var execution = await context.JobExecutions.FindAsync([executionId]);
        if (execution is not null)
        {
            execution.Status = status;
            execution.CompletedAt = completedAt;
            execution.Refreshed = refreshed;
            execution.Failed = failed;
        }

        await context.SaveChangesAsync();
    }
}

public enum DailyDigestResult
{
    Completed,
    LeaseUnavailable
}
