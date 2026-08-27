using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.SeasonTracking;

public sealed class RecordSeasonUncertaintyCommand(AppDbContext context)
{
    public async Task ExecuteAsync(Guid seasonId, FinaleEvidenceAssessment evidence)
    {
        var season = await context.Seasons.FindAsync(seasonId);
        if (season is null || season.CompletedAt is not null)
            return;

        season.UncertaintyReason = SeasonUncertaintyPolicy.Assess(evidence);
        await context.SaveChangesAsync();
    }
}
