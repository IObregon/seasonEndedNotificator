namespace SeasonEnded.Api.Notifications;

public static class RetryPolicy
{
    public static readonly int[] DelayMinutes = [0, 5, 30, 120, 720];
    public const int MaxAttempts = 5;

    public static DateTimeOffset? NextAttemptAt(int attemptNumber, DeliveryOutcome outcome)
    {
        if (outcome != DeliveryOutcome.TransientFailure || attemptNumber >= MaxAttempts)
            return null;

        var delayIndex = Math.Min(attemptNumber, DelayMinutes.Length - 1);
        return DateTimeOffset.UtcNow.AddMinutes(DelayMinutes[delayIndex]);
    }
}
