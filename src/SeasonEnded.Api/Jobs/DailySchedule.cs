namespace SeasonEnded.Api.Jobs;

public class DailyScheduleOptions
{
    public bool Enabled { get; init; }
    public int HourUtc { get; init; }
}

public static class DailySchedule
{
    public static bool IsDue(
        DailyScheduleOptions options,
        DateTimeOffset now,
        DateTimeOffset? lastCompletedAt)
    {
        if (!options.Enabled || now.Hour < options.HourUtc)
            return false;

        return lastCompletedAt is null || lastCompletedAt.Value.UtcDateTime.Date < now.UtcDateTime.Date;
    }
}
