using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Catalog;

namespace SeasonEnded.Api.SeasonTracking;

public sealed class ConfirmSeasonCompletionCommand(AppDbContext context)
{
    public async Task<ConfirmSeasonCompletionResult> ExecuteAsync(
        Guid seasonId,
        FinaleEvidence evidence,
        DateTimeOffset now)
    {
        var season = await FindIncompleteSeasonAsync(seasonId, evidence.SeasonNumber);
        if (season is null || !SeasonCompletionPolicy.IsEligible(evidence, now))
            return new ConfirmSeasonCompletionResult(Created: false);

        return await CompleteAsync(season, SeasonCompletionPolicy.CompletionTime(evidence));
    }

    public async Task<ConfirmSeasonCompletionResult> ExecuteAsync(
        Guid seasonId,
        DateOnlyFinaleEvidence evidence,
        DateTimeOffset now)
    {
        var season = await FindIncompleteSeasonAsync(seasonId, evidence.SeasonNumber);
        if (season is null || !DateOnlyCompletionPolicy.IsEligible(evidence, now))
            return new ConfirmSeasonCompletionResult(Created: false);

        return await CompleteAsync(season, DateOnlyCompletionPolicy.CompletionTime(evidence));
    }

    public async Task<ConfirmSeasonCompletionResult> ExecuteAsync(
        Guid seasonId,
        BatchReleaseEvidence evidence,
        DateTimeOffset now)
    {
        var season = await FindIncompleteSeasonAsync(seasonId, evidence.SeasonNumber);
        if (season is null || !BatchCompletionPolicy.IsEligible(evidence, now))
            return new ConfirmSeasonCompletionResult(Created: false);

        return await CompleteAsync(season, BatchCompletionPolicy.CompletionTime(evidence));
    }

    private async Task<Season?> FindIncompleteSeasonAsync(Guid seasonId, int seasonNumber)
    {
        var season = await context.Seasons.FindAsync(seasonId);
        return season is { CompletedAt: null } && season.Number == seasonNumber
            ? season
            : null;
    }

    private async Task<ConfirmSeasonCompletionResult> CompleteAsync(
        Season season,
        DateTimeOffset completedAt)
    {
        season.CompletedAt = completedAt;
        context.SeasonCompletionEvents.Add(new SeasonCompletionEvent
        {
            SeasonId = season.Id,
            CompletedAt = completedAt
        });
        await context.SaveChangesAsync();
        return new ConfirmSeasonCompletionResult(Created: true);
    }
}

public sealed record ConfirmSeasonCompletionResult(bool Created);
