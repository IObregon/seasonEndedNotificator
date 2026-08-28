using SeasonEnded.Api.Jobs;

namespace SeasonEnded.Api.Tests;

public sealed class DailyRefreshScheduleTests
{
    [Fact]
    public void Disabled_schedule_is_never_due()
    {
        var options = new MetadataRefreshOptions { Enabled = false, HourUtc = 7 };

        Assert.False(DailySchedule.IsDue(
            options,
            new DateTimeOffset(2026, 8, 27, 7, 0, 0, TimeSpan.Zero),
            lastCompletedAt: null));
    }

    [Fact]
    public void Enabled_schedule_runs_once_after_configured_hour()
    {
        var options = new MetadataRefreshOptions { Enabled = true, HourUtc = 7 };
        var now = new DateTimeOffset(2026, 8, 27, 8, 0, 0, TimeSpan.Zero);

        Assert.True(DailySchedule.IsDue(options, now, lastCompletedAt: null));
        Assert.False(DailySchedule.IsDue(options, now, now.AddMinutes(-30)));
        Assert.True(DailySchedule.IsDue(options, now, now.AddDays(-1)));
    }
}
