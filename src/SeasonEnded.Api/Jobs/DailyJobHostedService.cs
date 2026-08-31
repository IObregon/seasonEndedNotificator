using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Jobs;

public abstract class DailyJobHostedService<TOptions>(
    IServiceScopeFactory scopeFactory,
    IOptions<TOptions> options,
    TimeProvider timeProvider) : BackgroundService
    where TOptions : DailyScheduleOptions
{
    protected abstract string JobName { get; }

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
            .Where(e => e.JobName == JobName && e.Status.StartsWith("Completed"))
            .MaxAsync(e => (DateTimeOffset?)e.CompletedAt, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (!DailySchedule.IsDue(options.Value, now, lastCompleted))
            return;

        await RunJobAsync(scope, now, cancellationToken);
    }

    protected abstract Task RunJobAsync(
        IServiceScope scope, DateTimeOffset now, CancellationToken cancellationToken);
}
