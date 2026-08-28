using SeasonEnded.Api.Jobs;

namespace SeasonEnded.Api.Tests;

public sealed class DigestScheduleTests
{
    [Fact]
    public void IsDue_when_no_previous_run_and_hour_reached()
    {
        var options = new DigestScheduleOptions { Enabled = true, HourUtc = 9 };
        var now = new DateTimeOffset(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);

        Assert.True(DailySchedule.IsDue(options, now, null));
    }

    [Fact]
    public void Not_due_before_configured_hour()
    {
        var options = new DigestScheduleOptions { Enabled = true, HourUtc = 9 };
        var now = new DateTimeOffset(2026, 8, 28, 8, 59, 0, TimeSpan.Zero);

        Assert.False(DailySchedule.IsDue(options, now, null));
    }

    [Fact]
    public void Not_due_when_disabled()
    {
        var options = new DigestScheduleOptions { Enabled = false, HourUtc = 9 };
        var now = new DateTimeOffset(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);

        Assert.False(DailySchedule.IsDue(options, now, null));
    }

    [Fact]
    public void Not_due_when_already_ran_today()
    {
        var options = new DigestScheduleOptions { Enabled = true, HourUtc = 9 };
        var now = new DateTimeOffset(2026, 8, 28, 10, 0, 0, TimeSpan.Zero);
        var lastCompleted = new DateTimeOffset(2026, 8, 28, 9, 5, 0, TimeSpan.Zero);

        Assert.False(DailySchedule.IsDue(options, now, lastCompleted));
    }

    [Fact]
    public void Due_when_last_run_was_yesterday()
    {
        var options = new DigestScheduleOptions { Enabled = true, HourUtc = 9 };
        var now = new DateTimeOffset(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);
        var lastCompleted = new DateTimeOffset(2026, 8, 27, 9, 5, 0, TimeSpan.Zero);

        Assert.True(DailySchedule.IsDue(options, now, lastCompleted));
    }
}
