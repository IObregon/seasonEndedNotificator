using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Jobs;

namespace SeasonEnded.Api.Tests;

public sealed class DailyJobHostedServiceTests
{
    [Fact]
    public async Task IsDue_returns_true_when_no_previous_job_execution()
    {
        await using var context = CreateContext();

        var lastCompleted = await context.JobExecutions
            .Where(e => e.JobName == "daily-digest" && e.Status.StartsWith("Completed"))
            .MaxAsync(e => (DateTimeOffset?)e.CompletedAt);

        Assert.Null(lastCompleted);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
