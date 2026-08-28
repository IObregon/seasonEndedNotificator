namespace SeasonEnded.Api.Jobs;

public sealed class DigestScheduleOptions : DailyScheduleOptions
{
    public const string SectionName = "DigestSchedule";

    public DigestScheduleOptions() => HourUtc = 9;
}
