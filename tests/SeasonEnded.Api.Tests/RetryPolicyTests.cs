using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Tests;

public sealed class RetryPolicyTests
{
    private static DateTimeOffset ReferenceTime => new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private readonly RetryPolicy _policy = new(new FixedTimeProvider(ReferenceTime));

    [Fact]
    public void Transient_failure_schedules_next_attempt()
    {
        var next = _policy.NextAttemptAt(1, DeliveryOutcome.TransientFailure);
        Assert.NotNull(next);
        Assert.Equal(ReferenceTime.AddMinutes(5), next);
    }

    [Fact]
    public void Permanent_failure_schedules_no_retry()
    {
        var next = _policy.NextAttemptAt(1, DeliveryOutcome.PermanentFailure);
        Assert.Null(next);
    }

    [Fact]
    public void Success_schedules_no_retry()
    {
        var next = _policy.NextAttemptAt(1, DeliveryOutcome.Succeeded);
        Assert.Null(next);
    }

    [Fact]
    public void Max_attempts_stops_retry()
    {
        var next = _policy.NextAttemptAt(RetryPolicy.MaxAttempts, DeliveryOutcome.TransientFailure);
        Assert.Null(next);
    }

    [Fact]
    public void Delay_increases_with_attempts()
    {
        var first = _policy.NextAttemptAt(1, DeliveryOutcome.TransientFailure)!.Value;
        var second = _policy.NextAttemptAt(2, DeliveryOutcome.TransientFailure)!.Value;

        Assert.True(second > first);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
