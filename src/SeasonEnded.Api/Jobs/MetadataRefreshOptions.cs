namespace SeasonEnded.Api.Jobs;

public sealed class MetadataRefreshOptions
{
    public const string SectionName = "MetadataRefresh";

    public bool Enabled { get; init; }
    public int HourUtc { get; init; } = 7;
}

public static class DailyRefreshSchedule
{
    public static bool IsDue(
        MetadataRefreshOptions options,
        DateTimeOffset now,
        DateTimeOffset? lastCompletedAt)
    {
        if (!options.Enabled || now.Hour < options.HourUtc)
            return false;

        return lastCompletedAt is null || lastCompletedAt.Value.UtcDateTime.Date < now.UtcDateTime.Date;
    }
}
