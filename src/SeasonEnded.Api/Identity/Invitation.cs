namespace SeasonEnded.Api.Identity;

public sealed class Invitation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Email { get; init; } = "";
    public string TokenHash { get; init; } = "";
    public UserRole Role { get; init; } = UserRole.User;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; init; }
    public string? InvitedByUserId { get; init; }

    public bool IsExpired(DateTime now) => now >= ExpiresAt;

    public bool IsActive => Status == "Pending" && !IsExpired(DateTime.UtcNow);
}
