using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Jobs;

public sealed class DailyDigestJob(IServiceScopeFactory scopeFactory, AppDbContext context)
    : DailyJob(context)
{
    public const string JobName = "daily-digest";

    protected override string LeaseKey => JobName;

    public async Task<DailyDigestResult> RunAsync(
        string owner, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var execution = await AcquireLeaseAndStartAsync(owner, now, cancellationToken);
        if (IsLeaseUnavailable(execution))
            return DailyDigestResult.LeaseUnavailable;

        int sentCount, failedCount;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var prepare = scope.ServiceProvider.GetRequiredService<PrepareDigestCommand>();
            var send = scope.ServiceProvider.GetRequiredService<SendDigestCommand>();

            var digestDate = DateOnly.FromDateTime(now.Date);
            var deliveries = await prepare.ExecuteAsync(digestDate, cancellationToken);
            sentCount = 0;
            failedCount = 0;

            foreach (var delivery in deliveries)
            {
                var result = await send.ExecuteAsync(delivery.Id, cancellationToken);
                if (result.Sent) sentCount++;
                else failedCount++;
            }
        }
        catch
        {
            await RecordFinishInNewScopeAsync(scopeFactory, execution!.Id, now, "Failed", 0, 0);
            throw;
        }

        await RecordFinishInNewScopeAsync(scopeFactory, execution!.Id, now,
            failedCount == 0 ? "Completed" : "CompletedWithFailures",
            sentCount, failedCount);

        return DailyDigestResult.Completed;
    }

    private static async Task RecordFinishInNewScopeAsync(
        IServiceScopeFactory scopeFactory,
        Guid executionId, DateTimeOffset completedAt,
        string status, int refreshed, int failed)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var lease = await ctx.JobLeases.FindAsync([JobName]);
        if (lease is not null) lease.ExpiresAt = completedAt;

        var exec = await ctx.JobExecutions.FindAsync([executionId]);
        if (exec is not null)
        {
            exec.Status = status;
            exec.CompletedAt = completedAt;
            exec.Refreshed = refreshed;
            exec.Failed = failed;
        }

        await ctx.SaveChangesAsync();
    }
}

public enum DailyDigestResult
{
    Completed,
    LeaseUnavailable
}
