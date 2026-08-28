namespace SeasonEnded.Api.Jobs;

public sealed class MetadataRefreshOptions : DailyScheduleOptions
{
    public const string SectionName = "MetadataRefresh";

    public MetadataRefreshOptions() => HourUtc = 7;
}
