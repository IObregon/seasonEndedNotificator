namespace SeasonEnded.Api.Notifications;

public sealed class DeliveryAttempt
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DigestDeliveryId { get; init; }
    public DigestDelivery DigestDelivery { get; set; } = null!;
    public int AttemptNumber { get; init; }
    public string Outcome { get; set; } = "";
    public string? SanitizedError { get; set; }
    public DateTimeOffset AttemptedAt { get; init; } = DateTimeOffset.UtcNow;
}

public enum DeliveryOutcome
{
    Succeeded,
    TransientFailure,
    PermanentFailure
}
