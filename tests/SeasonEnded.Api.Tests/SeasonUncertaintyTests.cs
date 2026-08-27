using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Tests;

public sealed class SeasonUncertaintyTests
{
    [Theory]
    [InlineData(false, true, false, false, true, UncertaintyReason.MissingFinaleAuthority)]
    [InlineData(true, false, false, false, true, UncertaintyReason.MissingSchedule)]
    [InlineData(true, true, true, false, true, UncertaintyReason.ProviderMappingConflict)]
    [InlineData(true, true, false, true, true, UncertaintyReason.EpisodeCountConflict)]
    [InlineData(true, true, false, false, false, UncertaintyReason.MissingTimeZone)]
    public void Unsafe_evidence_has_stable_reason(
        bool hasAuthority,
        bool hasSchedule,
        bool mappingConflict,
        bool countConflict,
        bool hasTimeZone,
        UncertaintyReason expected)
    {
        var evidence = new FinaleEvidenceAssessment(
            hasAuthority, hasSchedule, mappingConflict, countConflict, hasTimeZone);

        Assert.Equal(expected, SeasonUncertaintyPolicy.Assess(evidence));
    }

    [Fact]
    public async Task Uncertainty_is_persisted_without_completion_then_cleared_by_valid_completion()
    {
        await using var context = CreateContext();
        var show = new Show { ProviderId = 82, Title = "Game of Thrones", Status = "Ended" };
        var season = new Season { Show = show, ProviderSeasonId = 8, Number = 8 };
        show.Seasons.Add(season);
        context.Shows.Add(show);
        await context.SaveChangesAsync();

        var unsafeEvidence = new FinaleEvidenceAssessment(false, true, false, false, true);
        await new RecordSeasonUncertaintyCommand(context).ExecuteAsync(season.Id, unsafeEvidence);

        Assert.Equal(UncertaintyReason.MissingFinaleAuthority, season.UncertaintyReason);
        Assert.Null(season.CompletedAt);
        Assert.Empty(context.SeasonCompletionEvents);

        var start = DateTimeOffset.UtcNow.AddHours(-2);
        var validEvidence = new FinaleEvidence(8, "regular", true, start, 60);
        var result = await new ConfirmSeasonCompletionCommand(context)
            .ExecuteAsync(season.Id, validEvidence, DateTimeOffset.UtcNow);

        Assert.True(result.Created);
        Assert.Null(season.UncertaintyReason);
        Assert.Single(context.SeasonCompletionEvents);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
