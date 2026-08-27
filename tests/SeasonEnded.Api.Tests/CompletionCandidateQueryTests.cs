using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Tests;

public sealed class CompletionCandidateQueryTests
{
    [Fact]
    public async Task Excludes_completion_before_follow_and_keeps_later_completion()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        var show = new Show { ProviderId = 82, Title = "Game of Thrones", Status = "Ended" };
        var oldSeason = new Season { Show = show, ProviderSeasonId = 7, Number = 7 };
        var futureSeason = new Season { Show = show, ProviderSeasonId = 8, Number = 8 };
        show.Seasons.AddRange([oldSeason, futureSeason]);
        var followedAt = DateTime.UtcNow;
        context.Users.Add(user);
        context.Shows.Add(show);
        context.ShowFollows.Add(new ShowFollow { UserId = user.Id, ShowId = show.Id, FollowedAt = followedAt });
        context.SeasonCompletionEvents.AddRange(
            new SeasonCompletionEvent { SeasonId = oldSeason.Id, CompletedAt = followedAt.AddDays(-1) },
            new SeasonCompletionEvent { SeasonId = futureSeason.Id, CompletedAt = followedAt.AddDays(1) });
        await context.SaveChangesAsync();

        var candidates = await new CompletionCandidateQuery(context).ForUserAsync(user.Id);

        var candidate = Assert.Single(candidates);
        Assert.Equal(futureSeason.Id, candidate.SeasonId);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
