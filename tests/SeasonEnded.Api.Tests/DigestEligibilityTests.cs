using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Notifications;
using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Tests;

public sealed class DigestEligibilityTests
{
    [Fact]
    public async Task Eligible_item_selected_after_follow_timestamp()
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

        var candidates = await new DigestEligibilityQuery(context)
            .ForUserAsync(user.Id, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.Single(candidates);
        Assert.Equal("Test Show", candidates[0].ShowTitle);
        Assert.Equal(1, candidates[0].SeasonNumber);
    }

    [Fact]
    public async Task Completion_before_follow_is_excluded()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        var show = new Show { Title = "Old Show", ProviderId = 2, Status = "Ended" };
        var season = new Season { ShowId = show.Id, Number = 1, EndDate = new DateOnly(2023, 1, 1), ProviderSeasonId = 2 };
        var follow = new ShowFollow { UserId = user.Id, ShowId = show.Id, FollowedAt = DateTime.UtcNow };
        var completion = new SeasonCompletionEvent
        {
            SeasonId = season.Id, CompletedAt = DateTimeOffset.UtcNow.AddDays(-30), ConfirmedAt = DateTimeOffset.UtcNow.AddDays(-30)
        };
        context.Users.Add(user);
        context.Shows.Add(show);
        context.Seasons.Add(season);
        context.ShowFollows.Add(follow);
        context.SeasonCompletionEvents.Add(completion);
        await context.SaveChangesAsync();

        var candidates = await new DigestEligibilityQuery(context)
            .ForUserAsync(user.Id, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task Already_delivered_item_is_excluded()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        var show = new Show { Title = "Delivered Show", ProviderId = 3, Status = "Ended" };
        var season = new Season { ShowId = show.Id, Number = 2, EndDate = new DateOnly(2024, 6, 1), ProviderSeasonId = 3 };
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

        var delivery = new DigestDelivery { UserId = user.Id, Channel = "Email", DigestDate = DateOnly.FromDateTime(DateTime.UtcNow) };
        delivery.Items.Add(new DigestItem { SeasonCompletionEventId = completion.Id });
        context.DigestDeliveries.Add(delivery);
        await context.SaveChangesAsync();

        var candidates = await new DigestEligibilityQuery(context)
            .ForUserAsync(user.Id, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.Empty(candidates);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
