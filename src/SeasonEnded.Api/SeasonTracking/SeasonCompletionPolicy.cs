namespace SeasonEnded.Api.SeasonTracking;

public sealed record FinaleEvidence(
    int SeasonNumber,
    string EpisodeType,
    bool ExplicitFinale,
    DateTimeOffset AirStart,
    int? RuntimeMinutes);

public static class SeasonCompletionPolicy
{
    private static readonly TimeSpan MissingRuntimeBuffer = TimeSpan.FromHours(2);

    public static bool IsEligible(FinaleEvidence evidence, DateTimeOffset now)
    {
        if (evidence.SeasonNumber <= 0 || evidence.EpisodeType != "regular" || !evidence.ExplicitFinale)
            return false;

        return now >= CompletionTime(evidence);
    }

    internal static DateTimeOffset CompletionTime(FinaleEvidence evidence)
    {
        var duration = evidence.RuntimeMinutes is > 0
            ? TimeSpan.FromMinutes(evidence.RuntimeMinutes.Value)
            : MissingRuntimeBuffer;
        return evidence.AirStart.Add(duration);
    }
}
