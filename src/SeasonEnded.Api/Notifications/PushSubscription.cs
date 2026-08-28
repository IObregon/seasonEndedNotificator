namespace SeasonEnded.Api.Notifications;

public sealed class PushSubscription
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public string Endpoint { get; init; } = "";
    public string P256DH { get; set; } = "";
    public string Auth { get; set; } = "";
    public string? Label { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset RegisteredAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSuccessAt { get; set; }
}
