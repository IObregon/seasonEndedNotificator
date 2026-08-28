using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Tests;

public sealed class RetryPolicyTests
{
    [Fact]
    public void Transient_failure_schedules_next_attempt()
    {
        var next = RetryPolicy.NextAttemptAt(1, DeliveryOutcome.TransientFailure);
        Assert.NotNull(next);
    }

    [Fact]
    public void Permanent_failure_schedules_no_retry()
    {
        var next = RetryPolicy.NextAttemptAt(1, DeliveryOutcome.PermanentFailure);
        Assert.Null(next);
    }

    [Fact]
    public void Success_schedules_no_retry()
    {
        var next = RetryPolicy.NextAttemptAt(1, DeliveryOutcome.Succeeded);
        Assert.Null(next);
    }

    [Fact]
    public void Max_attempts_stops_retry()
    {
        var next = RetryPolicy.NextAttemptAt(RetryPolicy.MaxAttempts, DeliveryOutcome.TransientFailure);
        Assert.Null(next);
    }

    [Fact]
    public void Delay_increases_with_attempts()
    {
        var first = RetryPolicy.NextAttemptAt(1, DeliveryOutcome.TransientFailure)!.Value;
        var second = RetryPolicy.NextAttemptAt(2, DeliveryOutcome.TransientFailure)!.Value;

        Assert.True(second > first);
    }
}
