using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        var dbName = Guid.NewGuid().ToString();
        var sender = new TestEmailSender();
        var services = CreateServices(dbName, sender);
        await using var provider = services.BuildServiceProvider();

        await SeedEligibleUserAsync(provider);

        var job = provider.GetRequiredService<DailyDigestJob>();
        var result = await job.RunAsync("test-owner", DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(DailyDigestResult.Completed, result);
        Assert.NotNull(sender.SentMessage);
        Assert.Equal("user@example.test", sender.SentMessage.To);
    }

    [Fact]
    public async Task Second_owner_is_rejected_while_lease_is_active()
    {
        var dbName = Guid.NewGuid().ToString();
        var sender = new TestEmailSender();
        var services = CreateServices(dbName, sender);
        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.JobLeases.Add(new JobLease
            {
                Name = DailyDigestJob.JobName,
                Owner = "running-owner",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
            });
            await context.SaveChangesAsync();
        }

        var job = provider.GetRequiredService<DailyDigestJob>();
        var result = await job.RunAsync("other-owner", DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(DailyDigestResult.LeaseUnavailable, result);
    }

    [Fact]
    public async Task Lease_recovered_after_expiry()
    {
        var dbName = Guid.NewGuid().ToString();
        var sender = new TestEmailSender();
        var services = CreateServices(dbName, sender);
        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.JobLeases.Add(new JobLease
            {
                Name = DailyDigestJob.JobName,
                Owner = "old-owner",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5)
            });
            await context.SaveChangesAsync();
        }

        var job = provider.GetRequiredService<DailyDigestJob>();
        var result = await job.RunAsync("new-owner", DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(DailyDigestResult.Completed, result);
    }

    private static ServiceCollection CreateServices(string dbName, TestEmailSender sender)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
        services.AddSingleton<IEmailSender>(sender);
        services.AddSingleton<ITelegramSender, UnconfiguredTelegramSender>();
        services.AddSingleton<IPushSender, UnconfiguredPushSender>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<RetryPolicy>();
        services.AddLogging();
        services.AddScoped<PrepareDigestCommand>();
        services.AddScoped<SendDigestCommand>();
        services.AddScoped<DailyDigestJob>();
        return services;
    }

    private static async Task SeedEligibleUserAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
    }
}
