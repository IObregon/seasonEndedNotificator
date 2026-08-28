namespace SeasonEnded.Api.Notifications;

public sealed class DigestDelivery
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public string Channel { get; init; } = "Email";
    public DateOnly DigestDate { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<DigestItem> Items { get; } = [];
}

public sealed class DigestItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DigestDeliveryId { get; init; }
    public DigestDelivery DigestDelivery { get; set; } = null!;
    public Guid SeasonCompletionEventId { get; init; }
}
