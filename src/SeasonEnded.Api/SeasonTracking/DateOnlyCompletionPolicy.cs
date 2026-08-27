namespace SeasonEnded.Api.SeasonTracking;

public sealed record DateOnlyFinaleEvidence(
    int SeasonNumber,
    string EpisodeType,
    bool ExplicitFinale,
    DateOnly AirDate,
    string TimeZoneId);

public static class DateOnlyCompletionPolicy
{
    public static bool IsEligible(
        DateOnlyFinaleEvidence evidence,
        DateTimeOffset now)
    {
        if (evidence.SeasonNumber <= 0 ||
            evidence.EpisodeType != "regular" ||
            !evidence.ExplicitFinale)
            return false;

        return now >= CompletionTime(evidence);
    }

    internal static DateTimeOffset CompletionTime(DateOnlyFinaleEvidence evidence)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(evidence.TimeZoneId);
        var nextMidnight = evidence.AirDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var offset = timeZone.GetUtcOffset(nextMidnight);
        return new DateTimeOffset(nextMidnight, offset);
    }
}
