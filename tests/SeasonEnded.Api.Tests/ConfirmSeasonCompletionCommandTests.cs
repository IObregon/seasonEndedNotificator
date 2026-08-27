using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Tests;

public sealed class ConfirmSeasonCompletionCommandTests
{
    [Fact]
    public async Task First_eligible_evaluation_persists_completion_and_one_event()
    {
        await using var context = CreateContext();
        var show = new Show { ProviderId = 82, Title = "Game of Thrones", Status = "Ended" };
        var season = new Season { Show = show, ProviderSeasonId = 8, Number = 8 };
        show.Seasons.Add(season);
        context.Shows.Add(show);
        await context.SaveChangesAsync();
        var start = DateTimeOffset.UtcNow.AddHours(-2);
        var evidence = new FinaleEvidence(8, "regular", true, start, 60);
        var completedAt = start.AddMinutes(60);

        var result = await new ConfirmSeasonCompletionCommand(context)
            .ExecuteAsync(season.Id, evidence, completedAt);

        Assert.True(result.Created);
        Assert.Equal(completedAt, season.CompletedAt);
        var completion = await context.SeasonCompletionEvents.SingleAsync();
        Assert.Equal(season.Id, completion.SeasonId);
        Assert.Equal(completedAt, completion.CompletedAt);
    }

    [Fact]
    public async Task Repeated_evaluation_creates_no_duplicate_event()
    {
        await using var context = CreateContext();
        var show = new Show { ProviderId = 82, Title = "Game of Thrones", Status = "Ended" };
        var season = new Season { Show = show, ProviderSeasonId = 8, Number = 8 };
        show.Seasons.Add(season);
        context.Shows.Add(show);
        await context.SaveChangesAsync();
        var start = DateTimeOffset.UtcNow.AddHours(-2);
        var evidence = new FinaleEvidence(8, "regular", true, start, 60);
        var command = new ConfirmSeasonCompletionCommand(context);

        await command.ExecuteAsync(season.Id, evidence, DateTimeOffset.UtcNow);
        var second = await command.ExecuteAsync(season.Id, evidence, DateTimeOffset.UtcNow);

        Assert.False(second.Created);
        Assert.Single(context.SeasonCompletionEvents);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
