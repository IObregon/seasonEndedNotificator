using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Jobs;

namespace SeasonEnded.Api.Tests;

public sealed class DailyMetadataRefreshJobTests
{
    [Fact]
    public async Task Active_lease_prevents_overlapping_run()
    {
        await using var context = CreateContext();
        var now = new DateTimeOffset(2026, 8, 27, 7, 0, 0, TimeSpan.Zero);
        context.JobLeases.Add(new JobLease
        {
            Name = DailyMetadataRefreshJob.JobName,
            Owner = "other",
            ExpiresAt = now.AddMinutes(5)
        });
        await context.SaveChangesAsync();
        var refresh = new RecordingRefresh();

        var result = await new DailyMetadataRefreshJob(context, refresh)
            .RunAsync("this", now, CancellationToken.None);

        Assert.Equal(DailyJobResult.LeaseUnavailable, result);
        Assert.Equal(0, refresh.Calls);
    }

    [Fact]
    public async Task Expired_lease_is_recovered_and_success_is_recorded()
    {
        await using var context = CreateContext();
        var now = new DateTimeOffset(2026, 8, 27, 7, 0, 0, TimeSpan.Zero);
        context.JobLeases.Add(new JobLease
        {
            Name = DailyMetadataRefreshJob.JobName,
            Owner = "old",
            ExpiresAt = now.AddMinutes(-1)
        });
        await context.SaveChangesAsync();
        var refresh = new RecordingRefresh();

        var result = await new DailyMetadataRefreshJob(context, refresh)
            .RunAsync("new", now, CancellationToken.None);

        Assert.Equal(DailyJobResult.Completed, result);
        Assert.Equal(1, refresh.Calls);
        var execution = await context.JobExecutions.SingleAsync();
        Assert.Equal("Completed", execution.Status);
        Assert.Equal(now, execution.CompletedAt);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class RecordingRefresh : IFollowedShowRefresh
    {
        public int Calls { get; private set; }

        public Task<RefreshFollowedShowsResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new RefreshFollowedShowsResult(1, 0));
        }
    }
}
