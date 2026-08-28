using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Notifications;
using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Tests;

public sealed class PrepareDigestCommandTests
{
    [Fact]
    public async Task Creates_one_delivery_per_eligible_user()
    {
        await using var context = CreateContext();
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

        var digestDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var deliveries = await new PrepareDigestCommand(context).ExecuteAsync(digestDate);

        Assert.Single(deliveries);
        Assert.Equal(user.Id, deliveries[0].UserId);
        Assert.Equal("Email", deliveries[0].Channel);
        Assert.Single(deliveries[0].Items);
        Assert.Equal(completion.Id, deliveries[0].Items[0].SeasonCompletionEventId);
    }

    [Fact]
    public async Task Repeated_prepare_returns_existing_delivery()
    {
        await using var context = CreateContext();
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

        var digestDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = await new PrepareDigestCommand(context).ExecuteAsync(digestDate);
        var second = await new PrepareDigestCommand(context).ExecuteAsync(digestDate);

        Assert.Equal(first[0].Id, second[0].Id);
        var totalDeliveries = await context.DigestDeliveries.CountAsync();
        Assert.Equal(1, totalDeliveries);
    }

    [Fact]
    public async Task User_without_eligible_items_gets_no_delivery()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var digestDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var deliveries = await new PrepareDigestCommand(context).ExecuteAsync(digestDate);

        Assert.Empty(deliveries);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options);
}
