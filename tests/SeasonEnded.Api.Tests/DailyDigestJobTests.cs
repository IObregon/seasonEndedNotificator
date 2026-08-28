using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Jobs;
using SeasonEnded.Api.Notifications;
using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Tests;

public sealed class DailyDigestJobTests
{
    [Fact]
    public async Task Job_prepares_and_sends_digest()
    {
        await using var context = CreateContext();
        var sender = new TestEmailSender();
        var user = new User { Email = "user@example.test" };
        var show = new Show { Title = "Test Show", ProviderId = 1, Status = "Ended" };
        var season = new Season { ShowId = show.Id, Number = 1, EndDate = new DateOnly(2024, 1, 1), ProviderSeasonId = 1 };
        var follow = new ShowFollow { UserId = user.Id, ShowId = show.Id, FollowedAt = DateTime.UtcNow.AddDays(-10) };
        var completion = new SeasonCompletionEvent
        {
            SeasonId = season.Id, CompletedAt = DateTimeOffset.UtcNow.AddDays(-1), ConfirmedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        context.Users.Add(user);
        context.Shows.Add(show);
        context.Seasons.Add(season);
        context.ShowFollows.Add(follow);
        context.SeasonCompletionEvents.Add(completion);
        await context.SaveChangesAsync();

        var prepare = new PrepareDigestCommand(context);
        var send = new SendDigestCommand(context, sender);
        var job = new DailyDigestJob(context, prepare, send);

        var result = await job.RunAsync("test-owner", DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(DailyDigestResult.Completed, result);
        Assert.NotNull(sender.SentMessage);
        Assert.Equal("user@example.test", sender.SentMessage.To);
    }

    [Fact]
    public async Task Second_owner_is_rejected_while_lease_is_active()
    {
        await using var context = CreateContext();
        var sender = new TestEmailSender();
        var prepare = new PrepareDigestCommand(context);
        var send = new SendDigestCommand(context, sender);

        context.JobLeases.Add(new JobLease
        {
            Name = DailyDigestJob.JobName,
            Owner = "running-owner",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
        });
        await context.SaveChangesAsync();

        var job = new DailyDigestJob(context, prepare, send);
        var result = await job.RunAsync("other-owner", DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(DailyDigestResult.LeaseUnavailable, result);
    }

    [Fact]
    public async Task Lease_recovered_after_expiry()
    {
        await using var context = CreateContext();
        var sender = new TestEmailSender();
        var prepare = new PrepareDigestCommand(context);
        var send = new SendDigestCommand(context, sender);
        var job = new DailyDigestJob(context, prepare, send);

        var now = DateTimeOffset.UtcNow;
        await job.RunAsync("owner-1", now, CancellationToken.None);

        var afterExpiry = now.AddMinutes(31);
        var result = await job.RunAsync("owner-2", afterExpiry, CancellationToken.None);

        Assert.Equal(DailyDigestResult.Completed, result);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options);
}
