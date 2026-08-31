using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.SeasonTracking;

public sealed class RefreshFinaleScheduleCommand(
    AppDbContext context,
    ILatestFinaleSchedule provider)
{
    public async Task<RefreshFinaleScheduleResult> ExecuteAsync(
        Guid seasonId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var season = await context.Seasons.FindAsync(seasonId)
            ?? throw new InvalidOperationException("Season does not exist.");
        if (season.CompletedAt is not null)
            return new RefreshFinaleScheduleResult(Completed: false);

        var refreshed = await provider.GetAsync(season.ProviderSeasonId, cancellationToken);
        var uncertainty = SeasonUncertaintyPolicy.Assess(refreshed.Assessment);

        season.RefreshFinaleSchedule(refreshed.AirStart, refreshed.RuntimeMinutes, uncertainty);

        if (uncertainty is not null)
        {
            await context.SaveChangesAsync(cancellationToken);
            return new RefreshFinaleScheduleResult(Completed: false);
        }

        var evidence = new FinaleEvidence(
            refreshed.SeasonNumber,
            refreshed.EpisodeType,
            refreshed.ExplicitFinale,
            refreshed.AirStart,
            refreshed.RuntimeMinutes);
        var completion = await new ConfirmSeasonCompletionCommand(context)
            .ExecuteAsync(seasonId, evidence, now);
        if (!completion.Created)
            await context.SaveChangesAsync(cancellationToken);
        return new RefreshFinaleScheduleResult(completion.Created);
    }
}

public sealed record RefreshFinaleScheduleResult(bool Completed);
