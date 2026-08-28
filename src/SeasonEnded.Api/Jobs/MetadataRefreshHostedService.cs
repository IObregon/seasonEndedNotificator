using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Jobs;

public sealed class MetadataRefreshHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<MetadataRefreshOptions> options,
    TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunIfDueAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(1), timeProvider, stoppingToken);
        }
    }

    private async Task RunIfDueAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lastCompleted = await db.JobExecutions
            .Where(execution =>
                execution.JobName == DailyMetadataRefreshJob.JobName &&
                execution.Status.StartsWith("Completed"))
            .MaxAsync(execution => execution.CompletedAt, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (!DailyRefreshSchedule.IsDue(options.Value, now, lastCompleted))
            return;

        var job = scope.ServiceProvider.GetRequiredService<DailyMetadataRefreshJob>();
        await job.RunAsync(Environment.MachineName, now, cancellationToken);
    }
}
