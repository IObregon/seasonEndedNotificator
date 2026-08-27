namespace SeasonEnded.Api.SeasonTracking;

public sealed class SeasonCompletionEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SeasonId { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
    public DateTimeOffset ConfirmedAt { get; init; } = DateTimeOffset.UtcNow;
}
