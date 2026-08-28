namespace SeasonEnded.Api.Jobs;

public sealed class DigestScheduleOptions
{
    public const string SectionName = "DigestSchedule";

    public bool Enabled { get; init; }
    public int HourUtc { get; init; } = 9;
}

public static class DigestSchedule
{
    public static bool IsDue(
        DigestScheduleOptions options,
        DateTimeOffset now,
        DateTimeOffset? lastCompletedAt)
    {
        if (!options.Enabled || now.Hour < options.HourUtc)
            return false;

        return lastCompletedAt is null || lastCompletedAt.Value.UtcDateTime.Date < now.UtcDateTime.Date;
    }
}
