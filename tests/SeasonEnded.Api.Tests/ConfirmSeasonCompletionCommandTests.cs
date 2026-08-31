using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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

        var result = await new ConfirmSeasonCompletionCommand(context, NullLogger<ConfirmSeasonCompletionCommand>.Instance)
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
        var command = new ConfirmSeasonCompletionCommand(context, NullLogger<ConfirmSeasonCompletionCommand>.Instance);

        await command.ExecuteAsync(season.Id, evidence, DateTimeOffset.UtcNow);
        var second = await command.ExecuteAsync(season.Id, evidence, DateTimeOffset.UtcNow);

        Assert.False(second.Created);
        Assert.Single(context.SeasonCompletionEvents);
    }

    [Fact]
    public async Task Date_only_finale_emits_once_after_local_midnight()
    {
        await using var context = CreateContext();
        var show = new Show { ProviderId = 82, Title = "Game of Thrones", Status = "Ended" };
        var season = new Season { Show = show, ProviderSeasonId = 8, Number = 8 };
        show.Seasons.Add(season);
        context.Shows.Add(show);
        await context.SaveChangesAsync();
        var evidence = new DateOnlyFinaleEvidence(
            8, "regular", true, new DateOnly(2026, 8, 27), "UTC");
        var command = new ConfirmSeasonCompletionCommand(context, NullLogger<ConfirmSeasonCompletionCommand>.Instance);

        var before = await command.ExecuteAsync(
            season.Id, evidence, new DateTimeOffset(2026, 8, 27, 23, 59, 59, TimeSpan.Zero));
        var first = await command.ExecuteAsync(
            season.Id, evidence, new DateTimeOffset(2026, 8, 28, 0, 0, 0, TimeSpan.Zero));
        var repeated = await command.ExecuteAsync(
            season.Id, evidence, new DateTimeOffset(2026, 8, 28, 1, 0, 0, TimeSpan.Zero));

        Assert.False(before.Created);
        Assert.True(first.Created);
        Assert.False(repeated.Created);
        Assert.Single(context.SeasonCompletionEvents);
    }

    [Fact]
    public async Task Complete_batch_emits_once_while_partial_batch_emits_none()
    {
        await using var context = CreateContext();
        var show = new Show { ProviderId = 82, Title = "Batch Show", Status = "Running" };
        var partialSeason = new Season { Show = show, ProviderSeasonId = 1, Number = 1 };
        var completeSeason = new Season { Show = show, ProviderSeasonId = 2, Number = 2 };
        show.Seasons.AddRange([partialSeason, completeSeason]);
        context.Shows.Add(show);
        await context.SaveChangesAsync();
        var now = new DateTimeOffset(2026, 8, 28, 0, 0, 0, TimeSpan.Zero);
        var partial = new BatchReleaseEvidence(1, true, 4, 8, new DateOnly(2026, 8, 27), "UTC");
        var complete = new BatchReleaseEvidence(2, true, 8, 8, new DateOnly(2026, 8, 27), "UTC");
        var command = new ConfirmSeasonCompletionCommand(context, NullLogger<ConfirmSeasonCompletionCommand>.Instance);

        var partialResult = await command.ExecuteAsync(partialSeason.Id, partial, now);
        var completeResult = await command.ExecuteAsync(completeSeason.Id, complete, now);
        var repeated = await command.ExecuteAsync(completeSeason.Id, complete, now.AddHours(1));

        Assert.False(partialResult.Created);
        Assert.True(completeResult.Created);
        Assert.False(repeated.Created);
        var completion = Assert.Single(context.SeasonCompletionEvents);
        Assert.Equal(completeSeason.Id, completion.SeasonId);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
