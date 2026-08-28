namespace SeasonEnded.Api.Identity;

public sealed class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Email { get; init; } = "";
    public UserRole Role { get; set; }
    public string Status { get; set; } = "Active";
    public string? PreferredLanguage { get; set; }
    public bool? EmailNotificationsEnabled { get; set; }
    public bool? TelegramNotificationsEnabled { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
