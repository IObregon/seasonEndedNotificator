using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Tests;

public sealed class RefreshFinaleScheduleCommandTests
{
    [Fact]
    public async Task Postponed_schedule_replaces_stale_time_and_withholds_completion()
    {
        await using var context = CreateContext();
        var season = await SeedSeasonAsync(context);
        var oldStart = new DateTimeOffset(2026, 8, 27, 20, 0, 0, TimeSpan.Zero);
        season.FinaleAirStart = oldStart;
        await context.SaveChangesAsync();
        var revisedStart = oldStart.AddDays(7);
        var provider = new StubSchedule(new RefreshedFinaleSchedule(
            season.ProviderSeasonId,
            season.Number,
            "regular",
            ExplicitFinale: true,
            revisedStart,
            RuntimeMinutes: 60,
            new FinaleEvidenceAssessment(true, true, false, false, true)));

        var result = await new RefreshFinaleScheduleCommand(context, provider)
            .ExecuteAsync(season.Id, oldStart.AddHours(2), CancellationToken.None);

        Assert.False(result.Completed);
        Assert.Equal(revisedStart, season.FinaleAirStart);
        Assert.Empty(context.SeasonCompletionEvents);
    }

    [Fact]
    public async Task Revised_schedule_completes_once_after_new_episode_end()
    {
        await using var context = CreateContext();
        var season = await SeedSeasonAsync(context);
        var revisedStart = new DateTimeOffset(2026, 9, 3, 20, 0, 0, TimeSpan.Zero);
        var provider = new StubSchedule(new RefreshedFinaleSchedule(
            season.ProviderSeasonId,
            season.Number,
            "regular",
            true,
            revisedStart,
            60,
            new FinaleEvidenceAssessment(true, true, false, false, true)));
        var command = new RefreshFinaleScheduleCommand(context, provider);

        var first = await command.ExecuteAsync(
            season.Id, revisedStart.AddMinutes(60), CancellationToken.None);
        var repeated = await command.ExecuteAsync(
            season.Id, revisedStart.AddHours(2), CancellationToken.None);

        Assert.True(first.Completed);
        Assert.False(repeated.Completed);
        Assert.Single(context.SeasonCompletionEvents);
    }

    [Fact]
    public async Task Provider_failure_preserves_stored_schedule()
    {
        await using var context = CreateContext();
        var season = await SeedSeasonAsync(context);
        var storedStart = new DateTimeOffset(2026, 8, 27, 20, 0, 0, TimeSpan.Zero);
        season.FinaleAirStart = storedStart;
        await context.SaveChangesAsync();
        var provider = new StubSchedule(new HttpRequestException("unavailable"));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            new RefreshFinaleScheduleCommand(context, provider)
                .ExecuteAsync(season.Id, storedStart.AddDays(1), CancellationToken.None));

        Assert.Equal(storedStart, season.FinaleAirStart);
        Assert.Empty(context.SeasonCompletionEvents);
    }

    [Fact]
    public async Task Contradictory_refresh_records_uncertainty_and_creates_no_candidate()
    {
        await using var context = CreateContext();
        var season = await SeedSeasonAsync(context);
        var start = new DateTimeOffset(2026, 8, 27, 20, 0, 0, TimeSpan.Zero);
        var provider = new StubSchedule(new RefreshedFinaleSchedule(
            season.ProviderSeasonId,
            season.Number,
            "regular",
            true,
            start,
            60,
            new FinaleEvidenceAssessment(true, true, false, true, true)));

        var result = await new RefreshFinaleScheduleCommand(context, provider)
            .ExecuteAsync(season.Id, start.AddDays(1), CancellationToken.None);

        Assert.False(result.Completed);
        Assert.Equal(UncertaintyReason.EpisodeCountConflict, season.UncertaintyReason);
        Assert.Empty(context.SeasonCompletionEvents);
    }

    private static async Task<Season> SeedSeasonAsync(AppDbContext context)
    {
        var show = new Show { ProviderId = 82, Title = "Game of Thrones", Status = "Ended" };
        var season = new Season { Show = show, ProviderSeasonId = 8, Number = 8 };
        show.Seasons.Add(season);
        context.Shows.Add(show);
        await context.SaveChangesAsync();
        return season;
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class StubSchedule : ILatestFinaleSchedule
    {
        private readonly RefreshedFinaleSchedule? schedule;
        private readonly Exception? error;

        public StubSchedule(RefreshedFinaleSchedule schedule) => this.schedule = schedule;
        public StubSchedule(Exception error) => this.error = error;

        public Task<RefreshedFinaleSchedule> GetAsync(
            int providerSeasonId,
            CancellationToken cancellationToken) => error is null
                ? Task.FromResult(schedule!)
                : Task.FromException<RefreshedFinaleSchedule>(error);
    }
}
