namespace SeasonEnded.Api.Catalog;

public sealed class ShowFollow
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public Guid ShowId { get; init; }
    public DateTime FollowedAt { get; init; } = DateTime.UtcNow;
}
