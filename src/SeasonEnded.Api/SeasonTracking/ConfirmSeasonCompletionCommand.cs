using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.SeasonTracking;

public sealed class ConfirmSeasonCompletionCommand(AppDbContext context)
{
    public async Task<ConfirmSeasonCompletionResult> ExecuteAsync(
        Guid seasonId,
        FinaleEvidence evidence,
        DateTimeOffset now)
    {
        var season = await context.Seasons.FindAsync(seasonId);
        if (season is null || season.CompletedAt is not null)
            return new ConfirmSeasonCompletionResult(Created: false);
        if (season.Number != evidence.SeasonNumber ||
            !SeasonCompletionPolicy.IsEligible(evidence, now))
            return new ConfirmSeasonCompletionResult(Created: false);

        var completedAt = SeasonCompletionPolicy.CompletionTime(evidence);
        season.CompletedAt = completedAt;
        context.SeasonCompletionEvents.Add(new SeasonCompletionEvent
        {
            SeasonId = seasonId,
            CompletedAt = completedAt
        });
        await context.SaveChangesAsync();
        return new ConfirmSeasonCompletionResult(Created: true);
    }

    public async Task<ConfirmSeasonCompletionResult> ExecuteAsync(
        Guid seasonId,
        DateOnlyFinaleEvidence evidence,
        DateTimeOffset now)
    {
        var season = await context.Seasons.FindAsync(seasonId);
        if (season is null || season.CompletedAt is not null)
            return new ConfirmSeasonCompletionResult(Created: false);
        if (season.Number != evidence.SeasonNumber ||
            !DateOnlyCompletionPolicy.IsEligible(evidence, now))
            return new ConfirmSeasonCompletionResult(Created: false);

        var completedAt = DateOnlyCompletionPolicy.CompletionTime(evidence);
        season.CompletedAt = completedAt;
        context.SeasonCompletionEvents.Add(new SeasonCompletionEvent
        {
            SeasonId = seasonId,
            CompletedAt = completedAt
        });
        await context.SaveChangesAsync();
        return new ConfirmSeasonCompletionResult(Created: true);
    }
}

public sealed record ConfirmSeasonCompletionResult(bool Created);
