namespace SeasonEnded.Api.SeasonTracking;

public sealed record BatchReleaseEvidence(
    int SeasonNumber,
    bool ExplicitFullSeasonRelease,
    int ReleasedEpisodeCount,
    int ExpectedEpisodeCount,
    DateOnly ReleaseDate,
    string TimeZoneId);

public static class BatchCompletionPolicy
{
    public static bool IsEligible(
        BatchReleaseEvidence evidence,
        DateTimeOffset now)
    {
        if (evidence.SeasonNumber <= 0 ||
            !evidence.ExplicitFullSeasonRelease ||
            evidence.ExpectedEpisodeCount <= 0 ||
            evidence.ReleasedEpisodeCount != evidence.ExpectedEpisodeCount)
            return false;

        return DateOnlyCompletionPolicy.IsEligible(ToDateOnlyEvidence(evidence), now);
    }

    internal static DateTimeOffset CompletionTime(BatchReleaseEvidence evidence) =>
        DateOnlyCompletionPolicy.CompletionTime(ToDateOnlyEvidence(evidence));

    private static DateOnlyFinaleEvidence ToDateOnlyEvidence(BatchReleaseEvidence evidence) =>
        new(
            evidence.SeasonNumber,
            "regular",
            ExplicitFinale: true,
            evidence.ReleaseDate,
            evidence.TimeZoneId);
}
