namespace SeasonEnded.Api.Identity;

public sealed class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Email { get; init; } = "";
    public UserRole Role { get; init; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
