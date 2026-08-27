namespace SeasonEnded.Api.Identity;

public sealed class MagicLinkToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public string TokenHash { get; init; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; init; }

    public bool IsExpired(DateTime now) => now >= ExpiresAt;
}
